using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_HP : MonoBehaviour
{
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp;

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
            case "G_Robo":
            case "T_Robo":
                if (anim != null)
                {
                    anim.SetBool("Death", true);
                }
                Debug.Log($"{t} ÇÕéÄñSÉAÉjÉÅ Å® îjâÛó\ñÒ");
                Destroy(obj, 5f);
                break;

            case "Fence2":
            case "Fence1":
            case "Land":
            case "S_Robo":
                Debug.Log($"{t} ÇîjâÛ");
                Destroy(obj, 5f);
                break;

            default:
                Debug.Log($"ñ¢ëŒâûÉ^ÉO({t})ÅFÇ∆ÇËÇ†Ç¶Ç∏îjâÛ");
                Destroy(obj, 5f);
                break;
        }
    }
}
