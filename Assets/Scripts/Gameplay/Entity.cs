using Mirror;
using UnityEngine;

public class Entity : EntityBase
{
    public int Team;
    public Renderer Renderer;

    public static int Team0 = 0;
    public static int Team1 = 1;
    public static int AnyTeam = -1;
    public static Color OwnColor = new Color(0.1f, 0.1f, 1f);
    public static Color EnemyColor = new Color(1.0f, 0.1f, 0.1f);

    protected GameWorld _world;
    private Material _materialInstance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected sealed override void Start()
    {
        _world = Get.Instance<GameWorld>();
        
        Init();

        // Cache material instance to ensure each entity has its own material
        if (Renderer != null)
        {
            _materialInstance = Renderer.material;
        }
    }

    // Update is called once per frame
    protected sealed override void Update()
    {
        DoUpdate(Time.deltaTime);
        UpdateColor();
    }

    protected sealed override void OnDestroy()
    {
        if (Get.ShuttingDown)
            return;

        DeInit();
    }

    private void UpdateColor()
    {
        if (_materialInstance == null)
            return;

        var color = GetColor;
        _materialInstance.color = color;
    }

    protected virtual Color GetColor => Team == 0 ? OwnColor : EnemyColor;

    protected virtual void Init() { }
    protected virtual void DoUpdate(float deltaTime) { }
    protected virtual void DeInit() { }
}

public abstract class EntityBase : NetworkBehaviour
{
    protected abstract void Start();
    protected abstract void Update();

    protected abstract void OnDestroy();
}
