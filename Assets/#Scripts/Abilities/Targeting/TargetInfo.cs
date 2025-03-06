using UnityEngine;

public class TargetInfo : MonoBehaviour
{
    [SerializeField] private float _losDistanceMult = 1.0f;
    [SerializeField] private Transform _focusT = null;

    public float LosDistanceMult { get { return _losDistanceMult; } set { _losDistanceMult = value; } }

    public Transform FocusT { get { return _focusT; } }

    public Vector3 FocusPos { get { return FocusT ? FocusT.transform.position : transform.position; } }
    public float FocusScale { get { return FocusT ? FocusT.transform.localScale.x : 1.0f; } }
}