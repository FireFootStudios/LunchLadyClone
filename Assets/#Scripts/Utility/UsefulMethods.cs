using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class Utils
{
    private static readonly System.Random rng = new System.Random();

    //Fisher - Yates shuffle
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    //Set next element in list if any
    public static T Next<T>(this IList<T> list, ref int from, bool loop = true)
    {
        if (list.Count == 0) return default(T);

        if (from >= list.Count - 1)
        {
            if (loop) from = 0;
        }
        else from++;

        return list[from];
    }

    //Set prev element in list if any
    public static T Prev<T>(this IList<T> list, ref int from, bool loop = true)
    {
        if (list.Count == 0 || from < 0) return default(T);

        if (from <= 0)
        {
            if (loop) from = list.Count - 1;
        }
        else from--;

        return list[from];
    }

    //Set next element in list if any
    public static T Next<T>(this IList<T> list, int from, bool loop = true)
    {
        if (list.Count == 0) return default(T);

        int next = from;

        if (next >= list.Count - 1)
        {
            if (loop) next = 0;
        }
        else next++;

        return list[next];
    }

    //Set prev element in list if any
    public static T Prev<T>(this IList<T> list, int from, bool loop = true)
    {
        if (list.Count == 0 || from < 0) return default(T);

        int next = from;

        if (next <= 0)
        {
            if (loop) next = list.Count - 1;
        }
        else next--;

        return list[next];
    }



    //Set next element in list if any
    public static T Next<T>(this IList<T> list, T from, bool loop = true)
    {
        int fromIndex = list.IndexOf(from);
        return list.Next(ref fromIndex, loop);
    }

    //Set prev element in list if any
    public static T Prev<T>(this IList<T> list, T from, bool loop = true)
    {
        int fromIndex = list.IndexOf(from);
        return list.Prev(ref fromIndex, loop);
    }


    public static Vector3 AbsVector(Vector3 vec)
    {
        vec.x = Mathf.Abs(vec.x);
        vec.y = Mathf.Abs(vec.y);
        vec.z = Mathf.Abs(vec.z);
        return vec;
    }

    public static bool Contains(this LayerMask mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }

    public static Transform GetTopLevelParent(Transform transform)
    {
        while (transform.parent != null)
            transform = transform.parent;

        return transform;
    }

    public static bool IsFloatZero(float value, float tolerance = 0.0001f)
    {
        return Mathf.Abs(value) <= tolerance;
    }

    public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Magnitude(ProjectPointLine(point, lineStart, lineEnd) - point);
    }

    public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 rhs = point - lineStart;
        Vector3 vector2 = lineEnd - lineStart;
        float magnitude = vector2.magnitude;
        Vector3 lhs = vector2;
        if (magnitude > 1E-06f)
        {
            lhs = (Vector3)(lhs / magnitude);
        }
        float num2 = Mathf.Clamp(Vector3.Dot(lhs, rhs), 0f, magnitude);
        return (lineStart + ((Vector3)(lhs * num2)));
    }

    public static string FormatTimer(double time)
    {
        int hours = (int)(time / 3600.0);
        int minutes = (int)((time % 3600.0) / 60.0);
        double seconds = time % 60.0;

        return string.Format("{0:D1}:{1:D2}:{2:00.00}", hours, minutes, seconds);
    }

    public static string FormatTimerMS(double time, bool cleanZero = false, bool hourseAsMinutes = false)
    {
        int hours = (int)(time / 3600.0);
        int minutes = (int)((time % 3600.0) / 60.0);
        float seconds = RoundToDecimals((float)(time % 60.0), 2, false);

        // Add hourse to minutes?
        if (hourseAsMinutes) minutes += (int)(hours * 60.0);

        string result;
        if (hourseAsMinutes || (cleanZero && hours == 0))
        {
            if (cleanZero && minutes == 0) result = string.Format("{0:0.00}", seconds);
            else result = string.Format("{0:D2}:{1:00.00}", minutes, seconds);
        }
        else
        {
            result = string.Format("{0:D1}:{1:D2}:{2:00}", hours, minutes, seconds);
        }

        return result;
    }

    public static string GetTimeAgoString(DateTime timestamp)
    {
        TimeSpan timeDifference = DateTime.UtcNow - timestamp;

        if (timeDifference.TotalMinutes < 1)
        {
            return "Less than 1 minute ago";
        }

        int days = timeDifference.Days;
        int hours = timeDifference.Hours;
        int minutes = timeDifference.Minutes;

        string result = "";

        if (days > 0)
        {
            result += $"{days} day{(days > 1 ? "s" : "")}";
        }

        if (hours > 0)
        {
            if (!string.IsNullOrEmpty(result)) result += ", ";
            result += $"{hours} hour{(hours > 1 ? "s" : "")}";
        }

        if (minutes > 0)
        {
            if (!string.IsNullOrEmpty(result)) result += ", ";
            result += $"{minutes} minute{(minutes > 1 ? "s" : "")}";
        }

        return result + " ago";
    }

    public static float RoundToDecimals(float value, int decimals, bool roundUp = false)
    {
        float multiplier = Mathf.Pow(10.0f, decimals);
        float roundedValue = roundUp ? Mathf.Ceil(value * multiplier) : Mathf.Floor(value * multiplier);
        return roundedValue / multiplier;
    }

    public static T RandomElement<T>(this IList<T> list, RandomisationSeed seed = null)
    {
        if (list == null || list.Count == 0)
        {
            throw new ArgumentException("List cannot be null or empty");
        }

        int index = seed != null ? seed.RandomRange(0, list.Count) : UnityEngine.Random.Range(0, list.Count);
        return list[index];
    }

    public static Vector3 EvaluateQuadraticCurve(Vector3 a, Vector3 b, float t, Vector3 controlePos)
    {
        Vector3 ac = Vector3.Lerp(a, controlePos, t);
        Vector3 cb = Vector3.Lerp(controlePos, b, t);
        return Vector3.Lerp(ac, cb, t);
    }

    public static float GetRandomFromBounds(Vector2 bounds, bool randomiseSign = false)
    {
        float value = UnityEngine.Random.Range(bounds.x, bounds.y);
        if (randomiseSign) value = UnityEngine.Random.Range(0, 2) == 0 ? value : -value;

        return value;
    }

    public static float GetRandomFromBounds(float min, float max, bool randomiseSign = false)
    {
        float value = UnityEngine.Random.Range(min, max);
        if (randomiseSign) value = UnityEngine.Random.Range(0, 2) == 0 ? value : -value;

        return value;
    }

    public static IEnumerator SetActiveDelayed(GameObject gameObject, float delay, bool value)
    {
        yield return new WaitForSeconds(delay);

        gameObject.SetActive(value);
    }

    public static Vector3 HorizontalVector(Vector3 vector)
    {
        vector.y = 0.0f;
        return vector;
    }

    public static Vector3 ScaledVector2Axis(Vector3 vector, float horizontal, float vertical)
    {
        Vector3 scaled = HorizontalVector(vector) * horizontal;
        scaled.y = vector.y * vertical;

        return scaled;
    }

    //Simple XOR encryption algorithm
    public static string EncryptOrDecrypt(string data, string keyWord)
    {
        string result = "";
        for (int i = 0; i < data.Length; i++)
        {
            result += (char)(data[i] ^ keyWord[i % keyWord.Length]);
        }
        return result;
    }

    public static Vector3 RandomPointInCollider(Collider collider)
    {
        if (collider == null) return Vector3.zero;

        Vector3 point = new Vector3(
            UnityEngine.Random.Range(collider.bounds.min.x, collider.bounds.max.x),
             UnityEngine.Random.Range(collider.bounds.min.y, collider.bounds.max.y),
             UnityEngine.Random.Range(collider.bounds.min.z, collider.bounds.max.z)
        );

        return point;
    }

    public static Vector3 RandomPointInCollider(Collider collider, RandomisationSeed randSeed = null)
    {
        if (collider == null) return Vector3.zero;

        if (randSeed == null) return RandomPointInCollider(collider);

        Vector3 point = new Vector3(
            randSeed.RandomRange(collider.bounds.min.x, collider.bounds.max.x),
             randSeed.RandomRange(collider.bounds.min.y, collider.bounds.max.y),
             randSeed.RandomRange(collider.bounds.min.z, collider.bounds.max.z)
        );

        return point;
    }

    public static bool IsPointInsideCollider(Vector3 point, Collider collider)
    {
        if (collider == null) return false;

        if (point.x < collider.bounds.min.x || point.x > collider.bounds.max.x) return false;
        if (point.y < collider.bounds.min.y || point.y > collider.bounds.max.y) return false;
        if (point.z < collider.bounds.min.z || point.z > collider.bounds.max.z) return false;

        return true;
    }

    public static bool IsPointInsideAnyColliders(Vector3 point, List<Collider> colliders)
    {
        if (colliders == null) return false;

        foreach (Collider collider in colliders)
            if (IsPointInsideCollider(point, collider))
                return true;

        return false;
    }

    public static bool IsPointInsideAllColliders(Vector3 point, List<Collider> colliders)
    {
        if (colliders == null) return false;

        foreach (Collider collider in colliders)
            if (!IsPointInsideCollider(point, collider))
                return false;

        return true;
    }

    public static bool AreFloatsEqual(float a, float b, float tolerance = 0.0001f)
    {
        return Mathf.Abs(a - b) < tolerance;
    }

    public static bool AreVectorsEqual(Vector3 a, Vector3 b, float tolerance = 0.01f)
    {
        if (!AreFloatsEqual(a.x, b.x, tolerance)) return false;
        if (!AreFloatsEqual(a.y, b.y, tolerance)) return false;
        if (!AreFloatsEqual(a.z, b.z, tolerance)) return false;
        return true;
    }

    public static int FloatAsInt(float value, int decimalAccuracy = 2)
    {
        return Mathf.RoundToInt(value * Mathf.Pow(10.0f, decimalAccuracy));
    }

    public static float IntAsFloat(int value, int decimalAccuracy = 2)
    {
        return value / Mathf.Pow(10.0f, decimalAccuracy);
    }

    public static int DoubleAsInt(double value, int decimalAccuracy = 2)
    {
        return (int)(value * Math.Pow(10.0, decimalAccuracy));
    }

    public static double IntAsDouble(int value, int decimalAccuracy = 2)
    {
        return value / Math.Pow(10.0, decimalAccuracy);
    }

    public static Transform GetUpperTransform(Transform from, int change)
    {
        if (from == null) return null;

        Transform t = from;
        int count = 0;
        while (t && count < change)
        {
            t = t.parent;
            count++;
        }

        return t;
    }

    public static Vector3 RoundVector(Vector3 value, int digits = 2)
    {
        Vector3 rounded;
        rounded.x = MathF.Round(value.x, digits);
        rounded.y = MathF.Round(value.y, digits);
        rounded.z = MathF.Round(value.z, digits);
        return rounded;
    }
    public static Vector2 RoundVector(Vector2 value, int digits = 2)
    {
        Vector2 rounded;
        rounded.x = MathF.Round(value.x, digits);
        rounded.y = MathF.Round(value.y, digits);
        return rounded;
    }

    //Not my code -> https://www.reddit.com/r/gamedev/comments/pphbhu/steamworks_leaderboard_question_vanishing_ugc/
    public static int[] UlongAsInts(ulong a)
    {
        int a1 = (int)(a & uint.MaxValue);
        int a2 = (int)(a >> 32);
        return new int[] { a1, a2 };
    }

    //Not my code -> https://www.reddit.com/r/gamedev/comments/pphbhu/steamworks_leaderboard_question_vanishing_ugc/
    public static ulong IntsAsUlong(int a1, int a2)
    {
        ulong b = (ulong)a2;
        b = b << 32;
        b = b | (uint)a1;
        return b;
    }

    public static string GenerateChecksum(string data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public static void SetToFill(RectTransform rectT)
    {
        if (!rectT) return;
        rectT.anchorMin = Vector2.zero;
        rectT.anchorMax = Vector2.one;
        rectT.offsetMin = rectT.offsetMax = Vector2.zero;
        rectT.pivot = new Vector2(0.5f, 0.5f);
    }

    public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
    {
        Vector3 AB = b - a;
        Vector3 AV = value - a;
        return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
    }

    public static T CopyComponent<T>(this T original, GameObject target) where T : Component
    {
        // Get the type of the original component
        var type = original.GetType();

        // Add a new component of the same type to the target GameObject
        var newComponent = target.AddComponent(type) as T;

        // Copy all fields from the original component to the new one
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            // Skip Unity-specific properties that shouldn't be copied
            if (field.Name == "name" || field.Name == "tag" || field.Name == "hideFlags")
                continue;

            field.SetValue(newComponent, field.GetValue(original));
        }

        // Copy all properties from the original component to the new one (if writable)
        //foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        //{
        //    if (property.CanWrite && property.GetSetMethod(true) != null)
        //    {
        //        property.SetValue(newComponent, property.GetValue(original));
        //    }
        //}

        return newComponent;
    }


    public static Vector3 PredictPosition(Vector3 targetPos, Vector3 fromPos, Vector3 targetVel, float fromSpeed)
    {
        Vector3 displacement = targetPos - fromPos;
        float targetMoveAngle = Vector3.Angle(-displacement, targetVel) * Mathf.Deg2Rad;
        float targetVelMagnitude = targetVel.magnitude;

        //if the target is stopping or if it is impossible for the projectile to catch up with the target (Sine Formula)
        if (targetVelMagnitude < 0.01f || (targetVelMagnitude > fromSpeed && Mathf.Sin(targetMoveAngle) / fromSpeed > Mathf.Cos(targetMoveAngle) / targetVelMagnitude))
        {
            //Debug.Log("Position prediction is not feasible.");
            return targetPos;
        }

        //also Sine Formula
        float shootAngle = Mathf.Asin(Mathf.Sin(targetMoveAngle) * targetVelMagnitude / fromSpeed);
        return targetPos + targetVel * displacement.magnitude / Mathf.Sin(Mathf.PI - targetMoveAngle - shootAngle) * Mathf.Sin(shootAngle) / targetVelMagnitude;
    }

    public static bool AreSpheresOverlapping(SphereCollider sphere1, SphereCollider sphere2)
    {
        // Get the world positions of the spheres
        Vector3 position1 = sphere1.transform.position + sphere1.center;
        Vector3 position2 = sphere2.transform.position + sphere2.center;

        // Calculate the distance between the two spheres
        float distance = Vector3.Distance(position1, position2);

        // Check if the distance is less than or equal to the sum of their radii
        float combinedRadius = sphere1.radius * sphere1.transform.lossyScale.x
                             + sphere2.radius * sphere2.transform.lossyScale.x;

        return distance <= combinedRadius;
    }
}