using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Karakteri sürekli olarak zemine yapıştırır.
/// NavMeshAgent'ı doğrudan kontrol ederek havada uçmayı önler.
/// </summary>
public class GroundSnapper : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Zemine yapıştırma aktif mi?")]
    public bool enableSnapping = true;
    
    [Tooltip("Raycast ile zemin araması yapılacak maksimum mesafe")]
    public float maxRaycastDistance = 10f;
    
    [Tooltip("Karakterin zemine olan offset'i (0 = tam zemine oturur)")]
    public float groundOffset = 0f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private NavMeshAgent navAgent;
    private Transform arenaTransform;
    private Transform groundSurface; // Pos_Red veya Pos_Blue
    private float lastSnapTime;
    private int snapFailCount = 0;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        
        // Arena'yı bul
        GameObject arena = GameObject.FindGameObjectWithTag("Arena");
        if (arena != null)
        {
            arenaTransform = arena.transform;
            
            // Takıma göre zemin yüzeyini bul (Pos_Red veya Pos_Blue)
            FindGroundSurface();
        }
        
        // NavMeshAgent ayarlarını düzelt
        if (navAgent != null)
        {
            navAgent.baseOffset = 0f;
            
            if (showDebugLogs)
            {
                Debug.Log($"<color=yellow>[GroundSnapper] {gameObject.name} - NavMeshAgent baseOffset=0 yapıldı</color>");
            }
        }
        
        // Hemen snap yap
        ForceSnapToGround();
    }

    void FindGroundSurface()
    {
        if (arenaTransform == null) return;
        
        // Karakterin tag'ine göre zemin yüzeyini bul
        string groundTag = "";
        if (gameObject.CompareTag("BlueTeam"))
        {
            groundTag = "BlueGround";
        }
        else if (gameObject.CompareTag("RedTeam"))
        {
            groundTag = "RedGround";
        }
        
        if (!string.IsNullOrEmpty(groundTag))
        {
            foreach (Transform child in arenaTransform)
            {
                if (child.CompareTag(groundTag))
                {
                    groundSurface = child;
                    if (showDebugLogs)
                    {
                        Debug.Log($"<color=cyan>[GroundSnapper] {gameObject.name} - Zemin yüzeyi bulundu: {child.name}, Y: {child.position.y}</color>");
                    }
                    break;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (!enableSnapping) return;
        
        // Her frame zemin kontrolü yap
        CheckAndSnapToGround();
    }

    void CheckAndSnapToGround()
    {
        if (arenaTransform == null)
        {
            // Arena'yı tekrar ara
            GameObject arena = GameObject.FindGameObjectWithTag("Arena");
            if (arena != null)
            {
                arenaTransform = arena.transform;
                FindGroundSurface(); // Zemin yüzeyini de bul
            }
            else
            {
                return;
            }
        }

        // Zemin yüzeyi henüz bulunamadıysa tekrar ara
        if (groundSurface == null)
        {
            FindGroundSurface();
        }

        // Mevcut pozisyon
        Vector3 currentPos = transform.position;
        
        // Zemin yüzeyinin Y pozisyonunu kullan (varsa), yoksa arena'nınkini
        float groundY = groundSurface != null ? groundSurface.position.y : arenaTransform.position.y;
        
        // Karakter zeminin üzerinde mi kontrol et
        float heightAboveGround = currentPos.y - groundY;
        
        // Eğer 0.3 birimden fazla yüksekteyse, aşağı indir
        if (heightAboveGround > 0.3f)
        {
            if (showDebugLogs && Time.time - lastSnapTime > 1f)
            {
                Debug.LogWarning($"<color=orange>[GroundSnapper] {gameObject.name} - HAVADA! Yükseklik: {heightAboveGround:F2}, Düzeltiliyor...</color>");
                lastSnapTime = Time.time;
            }
            
            ForceSnapToGround();
        }
    }

    public void ForceSnapToGround()
    {
        Vector3 currentPos = transform.position;
        float targetY = currentPos.y;
        bool foundGround = false;

        // Yöntem 0: Zemin yüzeyi Transform'unu kullan (EN GÜVENİLİR)
        if (groundSurface != null)
        {
            targetY = groundSurface.position.y + groundOffset;
            foundGround = true;
            
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.Warp(new Vector3(currentPos.x, targetY, currentPos.z));
            }
            else
            {
                transform.position = new Vector3(currentPos.x, targetY, currentPos.z);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"<color=green>[GroundSnapper] {gameObject.name} - Zemin Transform ile yerleştirildi. Y: {targetY:F2}</color>");
            }
            
            snapFailCount = 0;
            return;
        }

        // Yöntem 1: NavMesh kullan
        if (navAgent != null && navAgent.enabled)
        {
            // NavMeshAgent'ı geçici kapat
            bool wasEnabled = navAgent.enabled;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(currentPos, out hit, maxRaycastDistance, NavMesh.AllAreas))
            {
                targetY = hit.position.y + groundOffset;
                foundGround = true;
                
                // Pozisyonu güncelle
                if (wasEnabled && navAgent.isOnNavMesh)
                {
                    navAgent.Warp(new Vector3(currentPos.x, targetY, currentPos.z));
                }
                else
                {
                    transform.position = new Vector3(currentPos.x, targetY, currentPos.z);
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"<color=green>[GroundSnapper] {gameObject.name} - NavMesh ile zemine yerleştirildi. Y: {targetY:F2}</color>");
                }
                
                snapFailCount = 0;
                return;
            }
        }

        // Yöntem 2: Raycast kullan
        if (!foundGround)
        {
            Vector3 rayOrigin = currentPos + Vector3.up * 3f;
            RaycastHit rayHit;
            
            // Tüm layer'larda ara
            if (Physics.Raycast(rayOrigin, Vector3.down, out rayHit, maxRaycastDistance + 3f))
            {
                targetY = rayHit.point.y + groundOffset;
                foundGround = true;
                
                transform.position = new Vector3(currentPos.x, targetY, currentPos.z);
                
                if (showDebugLogs)
                {
                    Debug.Log($"<color=green>[GroundSnapper] {gameObject.name} - Raycast ile zemine yerleştirildi. Y: {targetY:F2}</color>");
                }
                
                snapFailCount = 0;
                return;
            }
        }

        // Yöntem 3: Arena Y pozisyonunu kullan (son çare)
        if (!foundGround && arenaTransform != null)
        {
            targetY = arenaTransform.position.y + groundOffset;
            transform.position = new Vector3(currentPos.x, targetY, currentPos.z);
            
            if (showDebugLogs)
            {
                Debug.Log($"<color=yellow>[GroundSnapper] {gameObject.name} - Arena Y'sine yerleştirildi. Y: {targetY:F2}</color>");
            }
            
            snapFailCount = 0;
            return;
        }

        // Hiçbir yöntem işe yaramadı
        snapFailCount++;
        if (snapFailCount > 10 && showDebugLogs)
        {
            Debug.LogError($"<color=red>[GroundSnapper] {gameObject.name} - ZEMIN BULUNAMADI! snapFailCount: {snapFailCount}</color>");
        }
    }

    // Dışarıdan çağrılabilir
    public void SetGroundOffset(float offset)
    {
        groundOffset = offset;
        ForceSnapToGround();
    }
}
