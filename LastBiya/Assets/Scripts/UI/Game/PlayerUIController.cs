using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField] PlayerUIView view;
    [SerializeField] PlayerController model;

    private bool initialized;

    void Start()
    {
        EventCenter.Instance.AddEventListener<IUnit>("HPChanged", OnHPChanged);
    }

    private void OnHPChanged(IUnit unit)
    {
        if (unit != (IUnit)model) return;
        view.UpdateHP(unit.CurrentHP / unit.MaxHP);
    }

    void Update()
    {
        if (!initialized && model != null && model.maxHP > 0)
        {
            float hp = model.currentHP / model.maxHP;
            view.UpdateHP(hp);
            view.playerHpBuffer.fillAmount = hp;
            initialized = true;
        }
    }

    void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener<IUnit>("HPChanged", OnHPChanged);
    }
}
