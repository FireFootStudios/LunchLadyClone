using Unity.Netcode.Components;
using UnityEngine;

public sealed class OwnerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}