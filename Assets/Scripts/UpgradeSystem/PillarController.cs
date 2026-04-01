using UnityEngine;

public class PillarController : MonoBehaviour
{
  
    public Transform spawnPoint;

    private GameObject _prefabToSpawn;
    private RoomBounds _roomBounds;
    private Upgrade _spawnedUpgrade; // Reference to the upgrade on top of this pillar
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

   public void Initialize(GameObject itemPrefab, RoomBounds room)
    {
        _prefabToSpawn = itemPrefab;
        _roomBounds = room;
    }

    // 2. CALL THIS FROM YOUR ANIMATION EVENT (at the end of "Rise" animation)
    public void SpawnItemAnimationEvent()
    {
        if (_prefabToSpawn != null)
        {
            
         
            GameObject item = Instantiate(_prefabToSpawn, spawnPoint.position, Quaternion.identity);

            // 3. Register the item back to RoomBounds so your existing selection logic works
            Upgrade upgradeScript = item.GetComponent<Upgrade>();
            if (upgradeScript != null && _roomBounds != null)
            {   
                upgradeScript.roomBounds = _roomBounds;
                _roomBounds.RegisterSpawnedUpgrade(upgradeScript);
                
                // Store reference to the spawned upgrade
                _spawnedUpgrade = upgradeScript;
            }
        }

    }
    
    // When player enters pillar's trigger, set this pillar's upgrade as the one in range
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && _spawnedUpgrade != null)
        {
            Upgrade.upgradeInRange = _spawnedUpgrade;
            _spawnedUpgrade.PlayerInRange = true;
            Debug.Log($"Player near pillar - upgrade in range: {_spawnedUpgrade.name}");
            
            // Trigger animation on the upgrade if it has animator
            Animator upgradeAnimator = _spawnedUpgrade.GetComponent<Animator>();
            if (upgradeAnimator != null)
            {
                upgradeAnimator.SetBool("PlayerInRange", true);
            }
            
            // Optional: pillar highlight animation
            if (_animator != null)
            {
                _animator.SetBool("PlayerInRange", true);
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && _spawnedUpgrade != null)
        {
            if (Upgrade.upgradeInRange == _spawnedUpgrade)
            {
                Upgrade.upgradeInRange = null;
            }
            _spawnedUpgrade.PlayerInRange = false;
            Debug.Log($"Player left pillar range");
            
            // Trigger animation on the upgrade if it has animator
            Animator upgradeAnimator = _spawnedUpgrade.GetComponent<Animator>();
            if (upgradeAnimator != null)
            {
                upgradeAnimator.SetBool("PlayerInRange", false);
            }
            
            // Optional: pillar highlight animation
            if (_animator != null)
            {
                _animator.SetBool("PlayerInRange", false);
            }
        }
    }
    
    // Called when upgrade is selected/destroyed
    public void ClearUpgradeReference()
    {
        _spawnedUpgrade = null;
    }
    
    private void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
        }
    }
}
