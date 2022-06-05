using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RandomExtensions
{
    // Array
    public static T[] Shuffle<T>(this T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            T temp = array[i];
            int randomIndex = Random.Range(i, array.Length);
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
        return array;
    }
    // List
    public static List<T> Shuffle<T>(this List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
        return list;
    }

    // Array
    public static int[] Distribute(int numerator, int denominator, int rangeMin, int rangeMax)
    {
        int[] array = new int[denominator];

        int remainder = numerator % denominator;
        int quotient = numerator / denominator;

        for (int i = 0; i < denominator; i++)
        {
            if (i < denominator - 1)
            {
                array[i] = i < remainder ? quotient + 1 : quotient;

                array[i] += Random.Range(rangeMin, rangeMax + 1);
            }
            else
            {
                array[i] = numerator - array.Sum();
            }
        }
        return array;
    }

    // WeightedFloat
    public static WeightedFloat ChooseWeightedFloat(List<WeightedFloat> weightedFloats, float? valueMin = null, float? valueMax = null)
    {
        List<WeightedFloat> filteredWFs = new List<WeightedFloat>();
        float totalWeights = 0;
        foreach (WeightedFloat weightedFloat in weightedFloats)
        {
            if (valueMin == null && valueMax == null)
            {
                totalWeights += weightedFloat.weight;

                filteredWFs = weightedFloats.ToList();
            }
            else if (valueMin != null && valueMax != null)
            {
                if (weightedFloat.value >= valueMin && weightedFloat.value <= valueMax)
                {
                    totalWeights += weightedFloat.weight;
                    filteredWFs.Add(weightedFloat);
                }
            }
            else if (valueMin != null)
            {
                if (weightedFloat.value >= valueMin)
                {
                    totalWeights += weightedFloat.weight;
                    filteredWFs.Add(weightedFloat);
                }
            }
            else if (valueMax != null)
            {
                if (weightedFloat.value <= valueMax)
                {
                    totalWeights += weightedFloat.weight;
                    filteredWFs.Add(weightedFloat);
                }
            }
        }

        float randomNumber = Random.Range(0, totalWeights);

        WeightedFloat selectedWeightedVal = filteredWFs[0];
        foreach (WeightedFloat WeightedFloat in filteredWFs)
        {
            if (randomNumber < WeightedFloat.weight)
            {
                selectedWeightedVal = WeightedFloat;
                break;
            }

            randomNumber -= WeightedFloat.weight;
        }
        return selectedWeightedVal;
    }

    public static List<float> Values(this List<WeightedFloat> weightedFloats)
    {
        List<float> values = new List<float>();
        foreach (WeightedFloat weightedFloat in weightedFloats)
        {
            values.Add(weightedFloat.value);
        }
        return values;
    }

    // WeightedVector3
    public static WeightedVector3 ChooseWeightedVector3(List<WeightedVector3> weightedVector3s)
    {
        List<WeightedVector3> filteredWV3s = weightedVector3s.ToList();

        float totalWeights = 0;
        foreach (WeightedVector3 weightedVector3 in weightedVector3s)
        {
            totalWeights += weightedVector3.weight;
        }

        float randomNumber = Random.Range(0, totalWeights);

        WeightedVector3 selectedWeightedVal = filteredWV3s[0];
        foreach (WeightedVector3 weightedVector3 in filteredWV3s)
        {
            if (randomNumber < weightedVector3.weight)
            {
                selectedWeightedVal = weightedVector3;
                break;
            }

            randomNumber -= weightedVector3.weight;
        }
        return selectedWeightedVal;
    }

    public static List<Vector3> Values(this List<WeightedVector3> weightedVector3s)
    {
        List<Vector3> values = new List<Vector3>();
        foreach(WeightedVector3 weightedVector3 in weightedVector3s)
        {
            values.Add(weightedVector3.value);
        }
        return values;
    }

    public static Vector3 GetPointInCollider(Collider collider)
    {
        Vector3 extents = collider.bounds.size / 2;
        Vector3 point = new Vector3(
            Random.Range(-extents.x, extents.x),
            Random.Range(-extents.y, extents.y),
            Random.Range(-extents.z, extents.z)
        ) + collider.bounds.center;
        return collider.transform.TransformPoint(point);
    }
    
    public static Vector3 GetSpawnPointInCollider(Collider collider, Vector3 direction, LayerMask ignoreLayers, Collider spawnCollider = null, Quaternion? spawnRotation = null)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = GetPointInCollider(collider);
            if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, ~ignoreLayers))
            {
                Debug.DrawLine(origin, hit.point, Color.blue, 10f);
                if (spawnCollider != null)
                {
                    Vector3 spawnPosition = hit.point + Vector3.up * (spawnCollider.bounds.size.y / 2 + 0.1f);
                    Quaternion rotation = spawnRotation == null ? spawnCollider.transform.rotation : (Quaternion)spawnRotation;
                    if (!Physics.CheckBox(spawnPosition, spawnCollider.bounds.size / 2, rotation, ~ignoreLayers))
                    {
                        return spawnPosition;
                    }
                }
                else
                {
                    return hit.point;
                }
            }
        }
        
        Debug.Log("No valid spawn point found in " + collider.name + "; returning Vector3.zero.");
        return Vector3.zero;
    }
}

[System.Serializable]
public struct WeightedFloat
{
    public float value;
    public float weight;
}

[System.Serializable]
public struct WeightedVector3
{
    public Vector3 value;
    public float weight;
}
