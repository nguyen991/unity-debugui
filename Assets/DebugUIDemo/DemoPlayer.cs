using System;
using System.Collections.Generic;
using Ng.DebugTools;
using UnityEngine;

/// <summary>
/// Demo for Ng Debug UI: a spinning square whose values are exposed in the "Player" panel.
/// Press ` (backquote) or tap the DBG button to open the debug UI.
/// </summary>
public class DemoPlayer : MonoBehaviour
{
    public enum Weapon { Sword, Bow, Staff }

    [Flags]
    public enum Cheats { None = 0, InfiniteAmmo = 1, NoClip = 2, OneHitKill = 4 }

    public int hp = 100;
    public int maxHp = 100;
    public float speed = 90f;          // degrees per second
    public int level = 1;
    public int gold = 120;
    public int score;                  // changes on its own: type in it to check edits aren't overwritten
    public float jumpHeight = 2.5f;
    public string playerName = "Hero";
    public bool godMode;
    public List<string> inventory = new List<string> { "Sword", "Shield" };
    public Weapon weapon;
    public Cheats cheats;
    public bool[] overlays = new bool[3];
    int potions;

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
        DebugUI.Panel(this, "Player", p => p
            .Label("Build 1.2.3")
            .Label("HP", () => hp)
            .Line()
            .Slider("Speed", () => speed, v => speed = v, 0, 720)
            .Slider("Level", () => level, v => level = v, 1, 10)
            .Field("Gold", () => gold, v => gold = v)
            .Field("Score", () => score, v => score = Mathf.Max(0, v)) // rejects negatives
            .Field("Jump", () => jumpHeight, v => jumpHeight = v)
            .Field("Name", () => playerName, v => playerName = v)
            .Field("God mode", () => godMode, v => godMode = v)
            .Toggles("Weapon", () => weapon, v => weapon = v)                       // enum: pick one
            .Toggles("Cheats", () => cheats, v => cheats = v)                       // [Flags] enum: pick several
            .Toggles("Overlay", new[] { "FPS", "Grid", "Hitbox" },
                i => overlays[i], (i, on) => overlays[i] = on, buttonWidth: 80)     // per-index bool
            .Space(12)
            .Horizontal(h => h
                .Button("Heal", () => hp = maxHp, new Color(0.2f, 0.6f, 0.3f))
                .Button("Kill", () => hp = 0, new Color(0.75f, 0.2f, 0.2f))
                .Line()
                .Button("Destroy", () => Destroy(gameObject)))
            .Foldout("Inventory", f => f
                .Each(() => inventory, (item, b) => b.Horizontal(h => h
                    .Label(item)
                    .Button("Drop", () => inventory.Remove(item))))
                .Button("Add potion", () => inventory.Add("Potion " + ++potions), new Color(0.3f, 0.45f, 0.8f)),
                open: true)
            .Foldout("Advanced", f => f
                .Slider("Gravity", () => Physics2D.gravity.y, v => Physics2D.gravity = new Vector2(0, v), -30, 0)
                .Label("Position", () => transform.position)));
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
            score += 10;
        }
    }
}
