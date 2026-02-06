using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_HP : MonoBehaviour
{
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp;
    public int Score;
    public int ClearFlag;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        currentHp = maxHp;
    }

    // Robotto1Wepon Ç©ÇÁåƒÇ—ÇΩÇ¢ÉÅÉ\ÉbÉh
    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        if (currentHp <= 0)
        {
            Kill(gameObject);
        }
    }

    public void Kill(GameObject obj)
    {
        if (obj == null) return;

        string t = obj.tag;

        switch (t)
        {
            case "Sphere":
                Score = Score + 500;
                ClearFlag++;
                Destroy(obj, 0.1f);

                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                break;

            case "G_Robo":
                Score=Score + 100;
                Destroy(obj, 0.1f);

                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                break;
            case "T_Robo":
                if (anim != null)
                {
                    anim.SetBool("Death", true);
                }
                Debug.Log($"{t} ÇÕéÄñSÉAÉjÉÅ Å® îjâÛó\ñÒ");

                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                Destroy(obj, 5f);
                break;

            case "Fence2":
                Destroy(obj, 0.1f);
                Score = Score + 50;

                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                break;
            case "Fence1":
                Score = Score + 20;
                Destroy(obj, 0.1f);

                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                break;
            case "Land":
                Debug.Log($"{t} ÇîjâÛ");
                Score = Score + 100;
                Destroy(obj, 0.1f);

                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                break;

            default:
                Debug.Log($"ñ¢ëŒâûÉ^ÉO({t})ÅFÇ∆ÇËÇ†Ç¶Ç∏îjâÛ");
                Destroy(obj, 0.1f);
                break;
        }
    }
}
