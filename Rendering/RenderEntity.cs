
public class RenderEntity : Entity
{
    public static int VisibleEntityCount = 0;

    public bool IsDrawable{get => true;}

    public bool IsTickable{get => Tickable();}

    public bool IsDestroyed{get;set;}

    protected bool Processable = true;

    public bool CanProcess{get => Processable;set => Processable = value;}

    public int ZValue {get;set;}

    protected bool Visible = true;

    public bool IsVisible {get => Visible;
    set
    {
        if (Visible && !value && Initialized) --VisibleEntityCount;
        if (!Visible && value && Initialized) ++VisibleEntityCount;
        Visible = value;
    }
    }

    protected virtual bool Tickable() => false;

    protected bool Initialized = false;

    public virtual void Init()
    {
        if (Visible) ++VisibleEntityCount;

        Initialized = true;
    }

    public virtual void EnterTree()
    {

    }

    public virtual void LeaveTree()
    {
        if(Visible) --VisibleEntityCount;
    }

    public virtual void Tick ()
    {

    }

    public virtual DrawableObject GetDrawable ()
    {
        return null;
    }
}