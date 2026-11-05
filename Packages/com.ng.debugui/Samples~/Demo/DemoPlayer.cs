using Ng.DebugUI;
using UnityEngine;

/// <summary>
/// Demo for Ng Debug UI: a spinning square whose values are exposed in the "Player" panel.
/// Press ` (backquote) or tap the DBG button to open the debug UI.
/// </summary>
public class DemoPlayer : MonoBehaviour
{
    public int hp = 100;
    public int maxHp = 100;
    public float speed = 90f;          // degrees per second
    public int level = 1;
    public int gold = 120;
    public float jumpHeight = 2.5f;
    public string playerName = "Hero";
    public bool godMode;

    float nextDrain;

    void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    void OnEnable()
    {
        // `this` is the owner: the panel disappears automatically when this object is destroyed.
        DebugUI.Panel(this, "Player", p =>
        {
            p.Label("Build 1.2.3");
            p.Label("HP", () => hp);
            p.Slider("Speed", () => speed, v => speed = v, 0, 720);
            p.Slider("Level", () => level, v => level = v, 1, 10);
            p.Field("Gold", () => gold, v => gold = v);
            p.Field("Jump", () => jumpHeight, v => jumpHeight = v);
            p.Field("Name", () => playerName, v => playerName = v);
            p.Field("God mode", () => godMode, v => godMode = v);
            p.Horizontal(h =>
            {
                h.Button("Heal", () => hp = maxHp);
                h.Button("Kill", () => hp = 0);
                h.Button("Destroy", () => Destroy(gameObject));
            });
            p.Foldout("Advanced", f =>
            {
                f.Slider("Gravity", () => Physics2D.gravity.y, v => Physics2D.gravity = new Vector2(0, v), -30, 0);
                f.Label("Position", () => transform.position);
            });
        });
    }

    void Start() => DemoTimePanel.Register();

    void Update()
    {
        transform.Rotate(0, 0, speed * Time.deltaTime);

        // The game changes hp on its own; the HP label follows it.
        if (!godMode && Time.time >= nextDrain)
        {
            nextDrain = Time.time + 1f;
            hp = Mathf.Max(0, hp - 1);
        }
    }
}
