using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MyUnityAddons
{
    namespace Calculations
    {
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

        public static class CustomRandom
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
            public static int[] Distribute(int numerator, int denominator, float mutateChance = 0, int rangeMin = 0, int rangeMax = 0)
            {
                int[] array = new int[denominator];
                int sum = 0;

                int remainder = numerator % denominator;
                int quotient = numerator / denominator;

                for (int i = 0; i < denominator; i++)
                {
                    if (i < denominator - 1)
                    {
                        array[i] = i < remainder ? quotient + 1 : quotient;

                        if (mutateChance > 0 && Random.value <= mutateChance)
                        {
                            array[i] += Random.Range(rangeMin, rangeMax + 1);
                        }
                        sum += array[i];
                    }
                    else
                    {
                        array[i] = numerator - sum;
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
                foreach (WeightedVector3 weightedVector3 in weightedVector3s)
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
                return point;
            }

            public static Vector3 GetSpawnPointInCollider(Collider collider, Vector3 direction, LayerMask ignoreLayers, BoxCollider spawnCollider = null, Quaternion? spawnRotation = null, bool onTopOfPoint = false)
            {
                for (int i = 0; i < 30; i++)
                {
                    Vector3 origin = GetPointInCollider(collider);
                    if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, ~ignoreLayers))
                    {
                        if (collider.bounds.Contains(hit.point))
                        {
                            if (spawnCollider != null)
                            {
                                Vector3 testPosition = hit.point - direction * (spawnCollider.size.y + 0.01f);

                                Debug.DrawLine(testPosition, origin, Color.red, 10f);
                                Quaternion rotation = spawnRotation == null ? spawnCollider.transform.rotation : (Quaternion)spawnRotation;
                                if (!Physics.CheckBox(testPosition, spawnCollider.size * 0.5f, rotation, ~ignoreLayers))
                                {
                                    if (onTopOfPoint)
                                    {
                                        return testPosition;
                                    }
                                    else
                                    {
                                        return hit.point;
                                    }
                                }
                            }
                            else
                            {
                                return hit.point;
                            }
                        }
                    }
                }

                Debug.Log("No valid spawn point found in " + collider.name + ", returning Vector3.zero.");
                return Vector3.zero;
            }
        }

        public static class CustomMath
        {
            public static float SqrDistance(Vector3 a, Vector3 b)
            {
                float xD = a.x - b.x;
                float yD = a.y - b.y; // optional
                float zD = a.z - b.z;
                return xD * xD + yD * yD + zD * zD; // or xD*xD + zD*zD
            }

            public static Vector3 Divide(this Vector3 a, Vector3 b)
            {
                return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
            }

            public static string FormattedTime(this float time)
            {
                float minutes = Mathf.FloorToInt(time / 60);
                float seconds = Mathf.FloorToInt(time % 60);
                float milliSeconds = time % 1 * 1000;

                return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliSeconds);
            }

            public static float FormattedAngle(float angle)
            {
                return angle > 180 ? angle - 360 : angle;
            }

            public static float ClampAngle(float angle, float min, float max)
            {
                return Mathf.Clamp(FormattedAngle(angle), min, max);
            }

            public static bool CanRotateTo(this Transform transform, Vector3 direction, float[] verticalAngleLimits, float[] horizontalAngleLimits)
            {
                float[] shootPositionAngles = new float[]
                {
                    Vector3.SignedAngle(transform.forward, direction, transform.up),
                    Vector3.SignedAngle(new Vector3(transform.forward.x, direction.y, transform.forward.z), transform.forward, transform.right),
                };

                return shootPositionAngles[0] > horizontalAngleLimits[0] && shootPositionAngles[0] < horizontalAngleLimits[1] && shootPositionAngles[1] > verticalAngleLimits[0] && shootPositionAngles[1] < verticalAngleLimits[1];
            }

            public static Sprite ImageToSprite(Texture2D imageTexture, float pixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
            {
                // Converting Texture2D to sprite
                return Sprite.Create(imageTexture, new Rect(0, 0, imageTexture.width, imageTexture.height), new Vector2(0, 0), pixelsPerUnit, 0, spriteType);
            }

            public static Sprite ImageToSprite(string filePath, float pixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
            {
                // Converting png or other image format to sprite
                Texture2D spriteTexture = new Texture2D(2, 2);
                if (File.Exists(filePath))
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    if (spriteTexture.LoadImage(fileData))
                    {
                        return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit, 0, spriteType);
                    }
                }
                return null;
            }

            public static Vector3 Round(this Vector3 vector3, int decimalPlaces = 1)
            {
                float multiplier = 1;
                if (decimalPlaces < 0)
                {
                    for (int i = 0; i > decimalPlaces; i++)
                    {
                        multiplier /= 10;
                    }
                }
                else
                {
                    for (int i = 0; i < decimalPlaces; i++)
                    {
                        multiplier *= 10;
                    }
                }

                return new Vector3(
                    Mathf.Round(vector3.x * multiplier) / multiplier,
                    Mathf.Round(vector3.y * multiplier) / multiplier,
                    Mathf.Round(vector3.z * multiplier) / multiplier
                    );
            }

            public static Vector3 FuturePosition(Vector3 currentPosition, Rigidbody rigidbody, float seconds)
            {
                return currentPosition + (rigidbody.velocity * seconds);
            }

            public static Vector3 FuturePosition(Vector3 currentPosition, Rigidbody rigidbody, int frames)
            {
                return currentPosition + (frames * Time.deltaTime * rigidbody.velocity);
            }

            public static float TravelTime(Vector3 start, Vector3 end, float speed)
            {
                return Vector3.Distance(start, end) / speed;
            }
        }

        public static class Extensions
        {
            public static Vector3[] Vertices(this Collider collider)
            {
                Vector3[] vertices;
                switch (collider)
                {
                    case SphereCollider sphereCollider:
                        Vector3 sphereCenter = sphereCollider.center;
                        vertices = new Vector3[]
                        {
                        new Vector3(sphereCenter.x, sphereCenter.y - sphereCollider.radius, sphereCenter.z),
                        new Vector3(sphereCenter.x, sphereCenter.y + sphereCollider.radius, sphereCenter.z),
                        new Vector3(sphereCenter.x + sphereCollider.radius, sphereCenter.y, sphereCenter.z),
                        new Vector3(sphereCenter.x - sphereCollider.radius, sphereCenter.y, sphereCenter.z),
                        new Vector3(sphereCenter.x, sphereCenter.y, sphereCenter.z + sphereCollider.radius),
                        new Vector3(sphereCenter.x, sphereCenter.y, sphereCenter.z - sphereCollider.radius),
                        };
                        return vertices;
                    case CapsuleCollider capsuleCollider:
                        Vector3 capsuleCenter = capsuleCollider.center;
                        vertices = new Vector3[]
                        {
                        new Vector3(capsuleCenter.x, capsuleCenter.y - capsuleCollider.height, capsuleCenter.z),
                        new Vector3(capsuleCenter.x, capsuleCenter.y + capsuleCollider.height, capsuleCenter.z),
                        new Vector3(capsuleCenter.x + capsuleCollider.radius, capsuleCenter.y, capsuleCenter.z),
                        new Vector3(capsuleCenter.x - capsuleCollider.radius, capsuleCenter.y, capsuleCenter.z),
                        new Vector3(capsuleCenter.x, capsuleCenter.y, capsuleCenter.z + capsuleCollider.radius),
                        new Vector3(capsuleCenter.x, capsuleCenter.y, capsuleCenter.z - capsuleCollider.radius),
                        };
                        return vertices;
                    case MeshCollider meshCollider:
                        vertices = meshCollider.sharedMesh.vertices;
                        return vertices;
                    default: // Box
                        Vector3 boxCenter = collider.bounds.center;
                        Vector3 extents = collider.bounds.extents;
                        vertices = new Vector3[]
                        {
                            collider.bounds.max,
                            boxCenter + new Vector3(-extents.x, extents.y, extents.z),
                            boxCenter + new Vector3(extents.x, extents.y, -extents.z),
                            boxCenter + new Vector3(-extents.x, extents.y, -extents.z),
                            boxCenter + new Vector3(extents.x, -extents.y, extents.z),
                            boxCenter + new Vector3(-extents.x, -extents.y, extents.z),
                            boxCenter + new Vector3(extents.x, -extents.y, -extents.z),
                            collider.bounds.min
                        };
                        return vertices;
                }
            }

            public static Transform ClosestTransform(this Transform fromTransform, List<Transform> transforms)
            {
                if (transforms.Count > 0)
                {
                    float bestDistance = CustomMath.SqrDistance(fromTransform.position, transforms[0].position);
                    Transform closestTransform = transforms[0];
                    for (int i = 1; i < transforms.Count; i++)
                    {
                        if (transforms[i] != null && transforms[i] != fromTransform)
                        {
                            float distance = CustomMath.SqrDistance(fromTransform.position, transforms[i].position);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                closestTransform = transforms[i];
                            }
                        }
                    }
                    return closestTransform;
                }
                return null;
            }

            public static Transform ClosestTransform(this Transform fromTransform, Transform parent)
            {
                Transform closestTransform = null;
                if (parent.childCount > 0)
                {
                    float bestDistance = Mathf.Infinity;
                    foreach (Transform child in parent)
                    {
                        if (child != fromTransform)
                        {
                            float distance = CustomMath.SqrDistance(fromTransform.position, child.position);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                closestTransform = child;
                            }
                        }
                    }
                }
                return closestTransform;
            }

            public static Vector3 ClosestAnglePosition(this Transform transform, List<Vector3> positions)
            {
                if (positions.Count > 0)
                {
                    float bestAngle = Vector3.Angle(positions[0] - transform.position, transform.forward);
                    Vector3 bestPosition = positions[0];
                    for (int i = 1; i < positions.Count; i++)
                    {
                        Vector3 direction = positions[i] - transform.position;
                        float angle = Vector3.Angle(direction, transform.forward);
                        if (angle < bestAngle)
                        {
                            bestAngle = angle;
                            bestPosition = positions[i];
                        }
                    }
                    return bestPosition;
                }
                return Vector3.zero;
            }

            public static Vector3 FarthestAnglePosition(this Transform transform, List<Vector3> positions)
            {
                if (positions.Count > 0)
                {
                    float bestAngle = Vector3.Angle(positions[0] - transform.position, transform.forward);
                    Vector3 bestPosition = positions[0];
                    for (int i = 1; i < positions.Count; i++)
                    {
                        Vector3 direction = positions[i] - transform.position;
                        float angle = Vector3.Angle(direction, transform.forward);
                        if (angle > bestAngle)
                        {
                            bestAngle = angle;
                            bestPosition = positions[i];
                        }
                    }
                    return bestPosition;
                }
                return Vector3.zero;
            }

            public static Transform ClosestAngleTransform(this Transform transform, List<Transform> transforms)
            {
                if (transforms.Count > 0)
                {
                    float bestAngle = Vector3.Angle(transforms[0].position - transform.position, transform.forward);
                    Transform bestTransform = transforms[0];
                    for (int i = 1; i < transforms.Count; i++)
                    {
                        Vector3 direction = transforms[i].position - transform.position;
                        float angle = Vector3.Angle(direction, transform.forward);
                        if (angle < bestAngle)
                        {
                            bestAngle = angle;
                            bestTransform = transforms[i];
                        }
                    }
                    return bestTransform;
                }
                return null;
            }

            public static Transform FarthestAngleTransform(this Transform transform, List<Transform> transforms)
            {
                if (transforms.Count > 0)
                {
                    float bestAngle = Vector3.Angle(transforms[0].position - transform.position, transform.forward);
                    Transform bestTransform = transforms[0];
                    for (int i = 1; i < transforms.Count; i++)
                    {
                        Vector3 direction = transforms[i].position - transform.position;
                        float angle = Vector3.Angle(direction, transform.forward);
                        if (angle > bestAngle)
                        {
                            bestAngle = angle;
                            bestTransform = transforms[i];
                        }
                    }
                    return bestTransform;
                }
                return null;
            }

            public static List<T1> Keys<T1, T2>(this List<KeyValuePair<T1, T2>> keyValuePairs)
            {
                List<T1> Keys = new List<T1>();
                foreach (KeyValuePair<T1, T2> pair in keyValuePairs)
                {
                    Keys.Add(pair.Key);
                }
                return Keys;
            }

            public static List<T2> Values<T1, T2>(this List<KeyValuePair<T1, T2>> keyValuePairs)
            {
                List<T2> Values = new List<T2>();
                foreach (KeyValuePair<T1, T2> pair in keyValuePairs)
                {
                    Values.Add(pair.Value);
                }
                return Values;
            }

            public static void AddOrReplace<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, T2 value)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }

            public static bool TryAdd<T>(this List<T> list, T item)
            {
                if (!list.Contains(item))
                {
                    list.Add(item);
                    return true;
                }
                return false;
            }
        }
    }

    namespace CustomConversions
    {
        using System;

        public static class Conversions
        {
            static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
            {
                if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
                if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
                if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

                // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
                int mag = (int)Math.Log(value, 1024);

                // 1L << (mag * 10) == 2 ^ (10 * mag) 
                // [i.e. the number of bytes in the unit corresponding to mag]
                decimal adjustedSize = (decimal)value / (1L << (mag * 10));

                // make adjustment when the value is large enough that
                // it would round up to 1000 or more
                if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
                {
                    mag += 1;
                    adjustedSize /= 1024;
                }

                return string.Format("{0:n" + decimalPlaces + "} {1}",
                    adjustedSize,
                    SizeSuffixes[mag]);
            }
        }
    }

    namespace CustomPhoton
    {
        using Photon.Pun;
        using Photon.Realtime;
        using Photon.Pun.UtilityScripts;
        using System.Runtime.Serialization.Formatters.Binary;

        public static class CustomNetworkHandling
        {
            public static Player[] NonSpectatorList
            {
                get
                {
                    return PhotonNetwork.PlayerList.Where((x) => !x.CustomProperties.ContainsKey("Spectator") || !(bool)x.CustomProperties["Spectator"]).ToArray();
                }
            }

            public static Player[] SpectatorList
            {
                get
                {
                    return PhotonNetwork.PlayerList.Where((x) => x.CustomProperties.ContainsKey("Spectator") && (bool)x.CustomProperties["Spectator"]).ToArray();
                }
            }

            public static Player FindPlayerWithUsername(string username)
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (player.NickName == username)
                    {
                        return player;
                    }
                }
                return null;
            }

            public static Player FindPlayerWithUserID(string UserID)
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    if (player.UserId == UserID)
                    {
                        return player;
                    }
                }
                return null;
            }

            public static List<PhotonView> AllPhotonViews(this Player player)
            {
                if (player != null)
                {
                    List<PhotonView> list = new List<PhotonView>();
                    int actorNr = player.ActorNumber;
                    for (int viewId = actorNr * PhotonNetwork.MAX_VIEW_IDS + 1; viewId < (actorNr + 1) * PhotonNetwork.MAX_VIEW_IDS; viewId++)
                    {
                        PhotonView photonView = PhotonView.Find(viewId);
                        if (photonView)
                        {
                            list.Add(photonView);
                        }
                    }
                    return list;
                }
                return null;
            }

            public static PhotonView FindPhotonView(this Player player)
            {
                try
                {
                    return PhotonView.Find((int)player.CustomProperties["ViewID"]);
                }
                catch
                {
                    return null;
                }
            }
        }

        public static class TeamAllocation
        {
            public static void JoinOrSwitchTeam(this Player player, string teamName)
            {
                PhotonTeam currentTeam = player.GetPhotonTeam();
                if (currentTeam != null)
                {
                    if (currentTeam.Name != teamName)
                    {
                        player.SwitchTeam(teamName);
                    }
                }
                else
                {
                    player.JoinTeam(teamName);
                }
            }

            public static void JoinOrSwitchTeam(this Player player, byte teamCode)
            {
                PhotonTeam currentTeam = player.GetPhotonTeam();
                if (currentTeam != null)
                {
                    if (currentTeam.Code != teamCode)
                    {
                        player.SwitchTeam(teamCode);
                    }
                }
                else
                {
                    player.JoinTeam(teamCode);
                }
            }

            public static void JoinOrSwitchTeam(this Player player, PhotonTeam team)
            {
                if (team == null)
                {
                    return;
                }

                PhotonTeam currentTeam = player.GetPhotonTeam();
                if (currentTeam != null)
                {
                    if (currentTeam != team)
                    {
                        player.SwitchTeam(team);
                    }
                }
                else
                {
                    player.JoinTeam(team);
                }
            }

            public static void AllocatePlayerToTeam(this Player player)
            {
                RoomSettings roomSettings = (RoomSettings)PhotonNetwork.CurrentRoom.CustomProperties["RoomSettings"];
                PhotonTeam currentTeam = player.GetPhotonTeam();

                switch (roomSettings.mode)
                {
                    case "Teams":
                        Dictionary<string, int> teamCounts = new Dictionary<string, int>()
                        {
                            { "Team 4", PhotonTeamsManager.Instance.GetTeamMembersCount("Team 4") },
                            { "Team 3", PhotonTeamsManager.Instance.GetTeamMembersCount("Team 3") },
                            { "Team 2", PhotonTeamsManager.Instance.GetTeamMembersCount("Team 2") },
                            { "Team 1", PhotonTeamsManager.Instance.GetTeamMembersCount("Team 1") },
                        };

                        if (teamCounts.Values.Sum() < roomSettings.playerLimit)
                        {
                            PhotonTeam smallestTeam = null;
                            foreach (string key in teamCounts.Keys)
                            {
                                if (smallestTeam == null)
                                {
                                    if (teamCounts[key] < roomSettings.teamSize)
                                    {
                                        PhotonTeamsManager.Instance.TryGetTeamByName(key, out smallestTeam);
                                    }
                                }
                                else
                                {
                                    if (teamCounts[smallestTeam.Name] < roomSettings.teamSize && teamCounts[key] <= teamCounts[smallestTeam.Name])
                                    {
                                        PhotonTeamsManager.Instance.TryGetTeamByName(key, out smallestTeam);
                                    }
                                }
                            }
                            if (smallestTeam != null)
                            {
                                if (currentTeam != null)
                                {
                                    if (currentTeam.Name == "Players")
                                    {
                                        player.SwitchTeam(smallestTeam);
                                    }
                                }
                                else
                                {
                                    player.JoinTeam(smallestTeam);
                                }
                            }
                        }
                        break;
                    default: // FFA, PvE, Co-Op
                        if (PhotonTeamsManager.Instance.GetTeamMembersCount("Players") < roomSettings.playerLimit)
                        {
                            if (currentTeam != null)
                            {
                                if (currentTeam.Name != "Players" && currentTeam.Name.Contains("Team"))
                                {
                                    player.SwitchTeam("Players");
                                }
                            }
                            else
                            {
                                player.JoinTeam("Players");
                            }
                        }
                        break;
                }

                player.JoinOrSwitchTeam("Spectators");
            }
        }

        public static class PhotonDataSerialization
        {
            public static byte[] ObjectToByteArray(object obj)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream memStream = new MemoryStream();

                formatter.Serialize(memStream, obj);
                return memStream.ToArray();
            }

            public static object ByteArrayToObject(byte[] arr)
            {
                MemoryStream memStream = new MemoryStream();

                BinaryFormatter formatter = new BinaryFormatter();
                memStream.Write(arr, 0, arr.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                object obj = formatter.Deserialize(memStream);
                return obj;
            }
        }
    }
}
