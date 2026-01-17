using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class Upgrade : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [SerializeField] private UpgradeSOBase upgradeSO;
    
    [Header("Selection Settings")]
    public float selectionRadius = 1.5f;
    public static Upgrade upgradeInRange;
    public bool PlayerInRange { get; private set; }
    private Animator animator;
    public RoomBounds roomBounds;
   
    void Awake()
    {
        animator = GetComponent<Animator>();
      

        // Set upgrade type for animator
        SetUpgradeTypeForAnimator();

        // Create trigger collider
        /*var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = selectionRadius;
        collider.isTrigger = true;*/
        
        
    }

    private void SetUpgradeTypeForAnimator()
    {
        if (upgradeSO is UpgradeHealth)
        {
            animator.SetFloat("UpgradeType", 1); // Health
        }
        else if (upgradeSO is UpgradeSpeed)
        {
            animator.SetFloat("UpgradeType", 2); // Speed
        }
      
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInRange = true;
            upgradeInRange = this;
            Debug.Log($"Player entered range of upgrade: {gameObject.name}");
            animator.SetBool("PlayerInRange", true);
        }
    }
    
      void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInRange = false;
            if (upgradeInRange == this)
            upgradeInRange = null;
            Debug.Log($"Player exited range of upgrade: {gameObject.name}");
            animator.SetBool("PlayerInRange", false);
        }
    }

    public void SelectThisUpgrade()
    {
        // Apply upgrade to player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) ApplyUpgrade(player);

        // Notify room that this upgrade was selected
        if (roomBounds != null) roomBounds.PlayerSelectedUpgrade(this);

        // Play selection animation
        animator.SetTrigger("Selected");

        // Disable collider to prevent multiple interactions
        GetComponent<Collider2D>().enabled = false;
        
    }
    
    public void ApplyUpgrade(GameObject player)
    {
        upgradeSO.ApplyUpgrade(player);
    }

    // Animation event called at the end of selected animation
    public void OnSelectedAnimationComplete()
    {
        Destroy(gameObject);
        
    }
    private void OnDestroy()
{
    if (upgradeInRange == this)
    {
        upgradeInRange = null;
    }
}
    // Animation event called at the end of disappear animation
    public void OnDisappearAnimationComplete()
    {
        Destroy(gameObject);
    }
}