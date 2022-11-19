using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MyUnityAddons.Calculations;
using Photon.Pun;

public class Trainer : MonoBehaviour
{
    [SerializeField] string modelFolder;
    [SerializeField] string fileName;
    [SerializeField] int[] layers;
    [SerializeField] int populationSize = 20;
    [SerializeField] GameObject botPrefab;
    [SerializeField] Transform botParent;
    [SerializeField] Transform toClearParent;
    [SerializeField] Transform[] destructables;
    [SerializeField] Vector3 spawnPosition;
    [SerializeField] Vector3 spawnEulers;

    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] int enemyCount = 5;
    [SerializeField] Collider spawnCollider;
    [SerializeField] LayerMask ignoreLayers;
    [SerializeField] [Range(0.0001f, 1)] float mutationChance = 0.05f;
    [SerializeField] [Range(0, 1)] float mutationStrength = 0.5f;
    [SerializeField] float gameSpeed = 1;
    [SerializeField] float timeFrame;

    List<NeuralNetwork> neuralNetworks;
    List<GeneticAlgorithmBot> bots;

    int generation = 0;

    private void Start()
    {
        PhotonNetwork.OfflineMode = true;
        // We want even population sizes
        if(populationSize % 2 != 0)
        {
            populationSize = 50;
        }

        InitNetworks();
        InvokeRepeating(nameof(InstantiateBots), 0.1f, timeFrame);
    }

    public void OnBotDeath()
    {
        foreach(GeneticAlgorithmBot bot in bots)
        {
            if(!bot.Dead)
            {
                return;
            }
        }

        CancelInvoke();
        InvokeRepeating(nameof(InstantiateBots), 0.1f, timeFrame);
    }

    void InitNetworks()
    {
        neuralNetworks = new List<NeuralNetwork>();
        for(int i = 0; i < populationSize; i++)
        {
            NeuralNetwork neuralNet = new NeuralNetwork(layers, NeuralNetwork.Activations.Tanh);
            neuralNet.Load(modelFolder + fileName);
            neuralNetworks.Add(neuralNet);
        }
    }

    void InstantiateBots()
    {
        Time.timeScale = gameSpeed;

        foreach(Transform child in toClearParent)
        {
            Destroy(child.gameObject);
        }
        foreach(Transform destructable in destructables)
        {
            destructable.GetChild(0).gameObject.SetActive(true);
        }

        if(enemyPrefabs.Length > 0)
        {
            for(int i = 0; i < enemyCount; i++)
            {
                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                GameObject newEnemy = Instantiate(prefab, CustomRandom.GetSpawnPointInCollider(spawnCollider, Vector3.down, ignoreLayers, prefab.transform.Find("Body").GetComponent<BoxCollider>(), Quaternion.identity), Quaternion.identity, toClearParent);
                newEnemy.GetComponent<TargetSystem>().chooseTarget = true;
                newEnemy.GetComponent<TargetSystem>().enemyParents.Add(botParent);
                newEnemy.GetComponent<FireControl>().bulletParent = toClearParent;
                if(newEnemy.TryGetComponent<MineControl>(out var mineControl))
                {
                    mineControl.mineParent = toClearParent;
                }
                newEnemy.GetComponent<BaseTankLogic>().effectsParent = toClearParent;
            }
        }

        if(bots != null)
        {
            SortNetworks();

            for(int i = 0; i < bots.Count; i++)
            {
                Destroy(bots[i].gameObject);
            }
        }

        bots = new List<GeneticAlgorithmBot>();
        for(int i = 0; i < populationSize; i++)
        {
            GeneticAlgorithmBot bot = Instantiate(botPrefab, spawnPosition, Quaternion.Euler(spawnEulers), botParent).GetComponent<GeneticAlgorithmBot>();
            bot.neuralNetwork = neuralNetworks[i];
            bot.trainer = this;
            bot.GetComponent<FireControl>().bulletParent = toClearParent;
            bot.GetComponent<MineControl>().mineParent = toClearParent;
            bot.GetComponent<BaseTankLogic>().effectsParent = toClearParent;
            bots.Add(bot);
        }
    }

    void SortNetworks()
    {
        float totalFitness = 0;
        foreach(GeneticAlgorithmBot bot in bots)
        {
            bot.UpdateFitness();
            totalFitness += bot.score;
        }

        neuralNetworks.Sort();
        neuralNetworks[^1].Save(modelFolder + fileName);
        Debug.Log("Gen " + generation + "\nAverage: " +(totalFitness / bots.Count) + ", Best: " + neuralNetworks[^1].fitness + ", Worst: " + neuralNetworks[0].fitness);

        generation++;
        for(int i = 0; i < populationSize / 2; i++)
        {
            neuralNetworks[i] = neuralNetworks[i + populationSize / 2].Copy(new NeuralNetwork(layers, NeuralNetwork.Activations.Tanh));
            neuralNetworks[i].Mutate(mutationChance, mutationStrength);
        }
    }
}
