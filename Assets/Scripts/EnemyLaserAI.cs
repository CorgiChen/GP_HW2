using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyLaserAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform firePoint;
    public LineRenderer lineRenderer;
    public ParticleSystem hitEffect;

    [Header("Ranges & Combat")]
    public float chaseRange = 15f;
    public float attackRange = 7f;
    public float fireCooldown = 2f;
    public float laserDuration = 0.5f;
    public float laserGrowSpeed = 50f;
    public float aimDelay = 0.15f;

    [Header("Audio")]
    public AudioClip fireSound;       // ✅ 發射音效
    private AudioSource audioSource;

    private NavMeshAgent agent;
    private Animator anim;
    private float nextFireTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D 音效
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 30f;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > chaseRange)
        {
            agent.isStopped = true;
            if (anim != null) anim.SetFloat("Speed", 0);
            return;
        }

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (anim != null)
                anim.SetFloat("Speed", agent.velocity.magnitude);
        }
        else
        {
            agent.isStopped = true;
            if (anim != null) anim.SetFloat("Speed", 0);

            Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookTarget - transform.position),
                Time.deltaTime * 10f
            );

            if (Time.time >= nextFireTime)
            {
                Vector3 dir = (player.position - firePoint.position).normalized;
                if (Physics.Raycast(firePoint.position, dir, out RaycastHit checkHit, attackRange))
                {
                    if (checkHit.collider.CompareTag("Player"))
                    {
                        StartCoroutine(FireLaser());
                        nextFireTime = Time.time + fireCooldown;
                    }
                    else
                    {
                        lineRenderer.enabled = false;
                    }
                }
            }
        }
    }

    IEnumerator FireLaser()
    {
        if (lineRenderer == null || player == null || firePoint == null) yield break;

        yield return new WaitForSeconds(aimDelay);

        Vector3 startPos = firePoint.position;
        Vector3 dir = (player.position - startPos).normalized;
        Vector3 endPos = startPos + dir * attackRange;

        if (Physics.Raycast(startPos, dir, out RaycastHit hit, attackRange))
        {
            endPos = hit.point;
        }

        // 🔊 先播放音效
        if (fireSound != null && audioSource != null)
            audioSource.PlayOneShot(fireSound);

        // ⏳ 延遲 0.5 秒再射擊
        yield return new WaitForSeconds(0.5f);

        // 若射中物體 → 播命中特效
        if (hitEffect != null && hit.collider != null)
        {
            hitEffect.transform.position = endPos;
            hitEffect.Play();
        }

        // 🔥 開始顯示雷射線
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, startPos);

        float totalDist = Vector3.Distance(startPos, endPos);
        float currentDist = 0f;

        while (currentDist < totalDist)
        {
            currentDist += laserGrowSpeed * Time.deltaTime;
            Vector3 currentEnd = startPos + dir * Mathf.Min(currentDist, totalDist);
            lineRenderer.SetPosition(1, currentEnd);
            yield return null;
        }

        yield return new WaitForSeconds(laserDuration);
        lineRenderer.enabled = false;
    }
}
