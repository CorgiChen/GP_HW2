using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false;

    [Header("Effects")]
    public ParticleSystem bloodEffectPrefab;  // 被擊中時生成的血特效
    public AudioClip hurtSound;               // 受傷音效
    public AudioClip deathSound;              // 死亡音效（可選）

    private Animator anim;
    private Collider col;
    private NavMeshAgent agent;
    private AudioSource audioSource;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        col = GetComponent<Collider>();
        agent = GetComponent<NavMeshAgent>();

        // 若沒有 AudioSource，自動加一個
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ✅ 被擊中時呼叫這個
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHealth -= amount;

        // 🩸 播放濺血特效
        if (bloodEffectPrefab != null)
        {
            ParticleSystem blood = Instantiate(
                bloodEffectPrefab,
                hitPoint,
                Quaternion.LookRotation(hitNormal)
            );
            blood.Play();
            Destroy(blood.gameObject, 2f);
        }

        // 🔊 播放受傷音效
        if (hurtSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }


        // ⚰️ 判定死亡
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log($"{gameObject.name} 死亡");

        // 停止移動與碰撞
        if (agent != null) agent.enabled = false;
        if (col != null) col.enabled = false;

        // 播放死亡音效
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);

        // 觸發死亡動畫
        if (anim != null)
            anim.SetTrigger("Die");

        // 播放完死亡動畫後停在最後一幀
        StartCoroutine(FreezeOnDeathEnd());
    }

    IEnumerator FreezeOnDeathEnd()
    {
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        anim.speed = 0;
    }
}
