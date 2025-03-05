using System.Collections;
using UnityEngine;

public class GunAbility : MonoBehaviour
{
    [SerializeField] private float _gunDamage = 1f;
    [SerializeField] private float _shotDelay = 1f;

    private Animator _harpoonAnimator = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _harpoonAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        ShootGun();
    }

    private void ShootGun()
    {
        if (Input.GetMouseButtonDown(0) )
        {
            Ray ray = Camera.allCameras[0].ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            _harpoonAnimator.SetTrigger("Shoot");

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.TryGetComponent<WhaleLogic>(out WhaleLogic whale))
                {
                    whale.Health.Add_Server(-_gunDamage, this.gameObject);
                    whale.Speed = whale.Speed + 1f;
                }
            }
        }
    }
}
