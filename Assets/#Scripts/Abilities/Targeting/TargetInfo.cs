using UnityEngine;

public class TargetInfo : MonoBehaviour
{
    [SerializeField] private float _losDistanceMult = 1.0f;

    public float LosDistanceMult { get { return _losDistanceMult; } set { _losDistanceMult = value; } }
}