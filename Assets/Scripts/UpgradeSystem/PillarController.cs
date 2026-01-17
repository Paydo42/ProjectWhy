using UnityEngine;

public class PillarController : MonoBehaviour
{
  
    public Transform spawnPoint;

    private GameObject _prefabToSpawn;
    private RoomBounds _roomBounds;

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
            }
        }

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
