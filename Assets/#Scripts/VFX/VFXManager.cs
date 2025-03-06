using System.Collections.Generic;
using UnityEngine;

public sealed class VFXManager : SingletonBase<VFXManager>
{
    //active/inactive vfx pool
    private List<VFXObject> _activeVFX = new List<VFXObject>();
    private Queue<VFXObject> _inactiveVFX = new Queue<VFXObject>();

    //extra vfx which will not be clean up/reused when inactive
    private List<VFXObject> _extraVFX = new List<VFXObject>();

    #region Publics

    public VFXObject PlayVFXSimple(ParticleSystem template, Vector3 pos, float lifeTime = 0.0f, float scale = 1.0f, bool allowRecycle = true)
    {
        if (!template) return null;

        //create a simple vfx data
        VFXData data = new VFXData();
        data.template = template;
        data.lifetime = lifeTime;
        data.extraScale = scale;
        data.useSourceScale = false;

        //get vfx object, set pos and play
        VFXObject vfxObject = GetVFXObject(data, allowRecycle);
        vfxObject.PS.transform.position = pos;
        vfxObject.Play();

        return vfxObject;
    }


    //If allow recycle is true (default), the vfx object will be managed by the vfx manager, meaning once it goes inactive due to the
    //source being null or the particle having finished, the vfx object will be removed from the active list and added to the pool for reuse.
    //Keep in mind that using this function, the particle will have to be started manually
    public VFXObject GetVFXObject(VFXData data, bool allowRecycle = false)
    {
        VFXObject vfxObject = GetVFXObject(data, allowRecycle ? _activeVFX : _extraVFX);

        return vfxObject;
    }

    public void StopAll(bool clearRemaining)
    {
        foreach (VFXObject vfxObject in _activeVFX)
        {
            vfxObject.Stop(clearRemaining);
            _inactiveVFX.Enqueue(vfxObject);
        }

        _activeVFX.Clear();
    }
    #endregion

    #region Privates
    private VFXObject GetVFXObject(VFXData data, List<VFXObject> targetList)
    {
        if (data.template == null) return null; 

        //check if there is a available, inactive vfx object in pool, if not create new one
        VFXObject vfxObject;
        if (_inactiveVFX.Count > 0)
        {
            //remove from inactive queue
            vfxObject = _inactiveVFX.Dequeue();

            //clean up prev particle if still exist (shouldnt tho)
            if (vfxObject.PS) Destroy(vfxObject.PS.gameObject);

            //(Re)init with data and instantiated ps
            vfxObject.Init(data, Instantiate(data.template));
        }
        else
        {
            //create vfxobject and add to list
            vfxObject = new VFXObject(data, Instantiate(data.template));
        }

        //add to active list
        targetList.Add(vfxObject);

        //attach particle system to vfx manager
        vfxObject.PS.transform.parent = this.gameObject.transform;

        return vfxObject;
    }

    private void Update()
    {
        //update active
        UpdateActiveVFXObjects();

        //update extra
        UpdateExtraVFXObjects();
    }

    private void UpdateActiveVFXObjects()
    {
        //update active vfx, if any is invalid, remove and add to inactive
        for (int i = 0; i < _activeVFX.Count; i++)
        {
            VFXObject vfxbject = _activeVFX[i];

            if (vfxbject == null || !vfxbject.Active)
            {
                //remove from list
                _activeVFX.Remove(vfxbject);

                //vfxObject might be null
                if (vfxbject != null)
                {
                    //add to queue
                    _inactiveVFX.Enqueue(vfxbject);

                    //clean up particle
                    Destroy(vfxbject.PS.gameObject);
                }
                
                i--;
                continue;
            }

            vfxbject.Update();
        }
    }

    private void UpdateExtraVFXObjects()
    {
        //update active vfx, if any is invalid, remove and add to inactive
        for (int i = 0; i < _extraVFX.Count; i++)
        {
            VFXObject vfxbject = _extraVFX[i];

            if (vfxbject == null)
            {
                _extraVFX.Remove(vfxbject);
                i--;
                continue;
            }

            vfxbject.Update();
        }
    }
    #endregion
}





/////////////////////////////////////////////
public sealed class VFXObject
{
    private Health _sourceHealth = null; //optional

    public VFXData Data { get; private set; }
    public ParticleSystem PS { get; private set; }
    public bool Active { get; private set; }
    public float LifeElapsed { get; private set; }

    public VFXObject(VFXData data, ParticleSystem ps)
    {
        Init(data, ps);
    }

    public void Init(VFXData data, ParticleSystem ps)
    {
        if (!ps) return;

        Data = data;
        PS = ps;

        //stop from playing on awake
        PS.Stop();

        if (Data.source)
        {
            _sourceHealth = Data.source.GetComponent<Health>();
        }
        else
        {
            _sourceHealth = null;
        }
    }

    public void Play()
    {
        //Start
        PS.Play();
        Active = true;
        LifeElapsed = 0.0f;

        //scale
        //PS.transform.localScale = Data.template.transform.localScale * Data.extraScale;
        //if (_sourceHealth && Data.useSourceScale) PS.transform.localScale *= _sourceHealth.FocusT.localScale.x;
        //else if (Data.source && Data.useSourceScale) PS.transform.localScale *= Data.source.transform.localScale.x;

        //initial update to prevent rendering before moved correctly
        Update(true);
    }

    public void Stop(bool clearRemaining)
    {
        if (clearRemaining) PS.Clear();
        PS.Stop();
        Active = false;
    }

    public void Update(bool initial = false)
    {
        if (!PS || !PS.IsAlive() || (Data.lifetime > 0.0f && LifeElapsed > Data.lifetime))
        {
            Active = false;
            return;
        }

        //update life elapsed
        LifeElapsed += Time.deltaTime;

        //if requires source and it is either null or dead, stop particle
        if (Data.requiresSource && (!Data.source || (_sourceHealth && _sourceHealth.IsDead)))
        {
            Stop(false);
        }
        else if (Data.source)
        {
            //update position (or set initially)
            if (Data.updateSourcePos || initial)
            {
                ////update position with source
                //Vector3 targetPos = _sourceHealth ? _sourceHealth.FocusPos : Data.source.transform.position;

                ////ignore y?
                //if (_sourceHealth && Data.ignoreFocusTHeight) targetPos.y = Data.source.transform.position.y;

                //PS.transform.position = targetPos + Data.template.transform.localPosition;
            }

            //update rotation (or set initially)
            if (Data.updateSourceRot || initial)
                PS.transform.rotation = Data.source.transform.rotation * Data.template.transform.rotation;
        }
    }
}

[System.Serializable]
public sealed class VFXData
{
    public GameObject source = null;
    public ParticleSystem template = null;

    public bool requiresSource = false;
    public bool updateSourcePos = true;
    public bool updateSourceRot = true;
    public bool useSourceScale = true;

    public bool ignoreFocusTHeight = true;
    public float extraScale = 1.0f;
    public float lifetime = 0.0f;
}