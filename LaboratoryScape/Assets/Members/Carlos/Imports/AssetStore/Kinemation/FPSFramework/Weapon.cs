// Designed by Kinemation, 2022

using System.Collections.Generic;
using Kinemation.FPSFramework.Runtime.Core;
using UnityEngine;

namespace Demo.Scripts.Runtime
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private List<Transform> scopes;
        [SerializeField] public WeaponAnimData gunData;
        [SerializeField] public RecoilAnimData recoilData;
        
        public FireMode fireMode;
        public float fireRate;
        public int burstAmount;

        private Animator _animator;
        private int _scopeIndex;
        
        /////////////////////////////////////////////
        /////////////////////////////////////////////
        
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask enemyColider;
        [SerializeField] private float impactForce;

        [Header("--- BLOOD ---")]
        public Light DirLight;
        public GameObject BloodAttach;
        public GameObject[] BloodFX;
        public Vector3 direction;
        int effectIdx;
        
        /////////////////////////////////////////////
        /////////////////////////////////////////////

        private void Start()
        {
            _animator = GetComponent<Animator>();
        }

        public Transform GetScope()
        {
            _scopeIndex++;
            _scopeIndex = _scopeIndex > scopes.Count - 1 ? 0 : _scopeIndex;
            return scopes[_scopeIndex];
        }
        
        public void OnFire()
        {
            RaycastHit hit = new RaycastHit();
            Ray ray = new Ray(muzzle.position, muzzle.forward);

            if (Physics.Raycast(ray, out hit, 100f, enemyColider, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("EnemyColliders"))
                {
                    hit.collider.GetComponentInParent<EnemyDespossess>().EnemyDie();
                    hit.collider.GetComponent<Rigidbody>().AddForce(-hit.normal * impactForce);
                    
                    /////////////////////////////////////////////////////////////////////////////
                    /////////////////////////////////////////////////////////////////////////////
                    
                    // var randRotation = new Vector3(0, Random.value * 360f, 0);
                    // var dir = CalculateAngle(Vector3.forward, hit.normal);
                    float angle = Mathf.Atan2(hit.normal.x, hit.normal.z) * Mathf.Rad2Deg + 180;

                    //var effectIdx = Random.Range(0, BloodFX.Length);
                    if (effectIdx == BloodFX.Length) effectIdx = 0;

                    var instance = Instantiate(BloodFX[effectIdx], hit.point, Quaternion.Euler(0, angle + 90, 0));
                    effectIdx++;

                    var settings = instance.GetComponent<BFX_BloodSettings>();
                    //settings.FreezeDecalDisappearance = InfiniteDecal;
                    settings.LightIntensityMultiplier = DirLight.intensity;

                    var attachBloodInstance = Instantiate(BloodAttach);
                    var bloodT = attachBloodInstance.transform;
                    bloodT.position = hit.point;
                    bloodT.localRotation = Quaternion.identity;
                    bloodT.localScale = Vector3.one * Random.Range(0.75f, 1.2f);
                    bloodT.LookAt(hit.point + hit.normal, direction);
                    bloodT.Rotate(90, 0, 0);
                    bloodT.transform.parent = hit.transform;
                    //Destroy(attachBloodInstance, 20);
                }
            }

            Debug.DrawRay(muzzle.position, muzzle.forward * 100, Color.red, 4);
            PlayFireAnim();
        }

        private void PlayFireAnim()
        {
            if (_animator == null)
            {
                return;
            }
            _animator.Play("Fire", 0, 0f);
        }
    }
}
