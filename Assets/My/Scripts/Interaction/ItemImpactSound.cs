using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemImpactSound : MonoBehaviour
{
    [SerializeField] private AudioClip[] impactClips;
    [SerializeField] private float minImpactVelocity = 1.5f;
    [SerializeField] private float cooldown = 0.2f;

    private float nextAllowedTime;
    private int lastClipIndex = -1;

    private void OnCollisionEnter(Collision collision)
    {
        if (impactClips == null || impactClips.Length == 0) return;
        if (Time.time < nextAllowedTime) return;
        if (collision.relativeVelocity.magnitude < minImpactVelocity) return;

        nextAllowedTime = Time.time + cooldown;

        int index = Random.Range(0, impactClips.Length);
        if (impactClips.Length > 1)
        {
            while (index == lastClipIndex)
                index = Random.Range(0, impactClips.Length);
        }
        lastClipIndex = index;

        AudioSource.PlayClipAtPoint(impactClips[index], collision.GetContact(0).point);
    }
}
