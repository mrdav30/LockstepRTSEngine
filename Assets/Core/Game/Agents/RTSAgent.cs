using System.Collections.Generic;
using UnityEngine;
using RTSLockstep;
using Newtonsoft.Json;

// RTS integration ability
public class RTSAgent : LSAgent
{
    #region Properties
    public string objectName;
    public int cost, sellValue;
    public int provisionCost;

    public bool _provisioned { get; private set; }
    private Rect _playingArea = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
    private List<Material> oldMaterials = new List<Material>();
    protected bool loadedSavedValues = false;
    protected AgentCommander cachedCommander;
    #endregion

    #region MonoBehavior
    //start
    public override void Initialize(
        Vector2d position = default(Vector2d),
        Vector2d rotation = default(Vector2d))
    {
        base.Initialize(position, rotation);
        SetCommander();
        if (cachedCommander)
        {
            if (!loadedSavedValues)
            {
                SetTeamColor();
            }
        }
    }

    public override void Simulate()
    {
        if (!_provisioned)
        {
            _provisioned = true;
            cachedCommander.AddResource(ResourceType.Army, provisionCost);
        }
        base.Simulate();
    }
    #endregion

    #region Public
    public void SetCommander()
    {
        cachedCommander = Controller.Commander;
    }

    public void SetTransparentMaterial(Material material, bool storeExistingMaterial)
    {
        if (storeExistingMaterial)
        {
            oldMaterials.Clear();
        }
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (storeExistingMaterial)
            {
                oldMaterials.Add(renderer.material);
            }
            renderer.material = material;
        }
    }

    public void RestoreMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (oldMaterials.Count == renderers.Length)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = oldMaterials[i];
            }
        }
    }

    public void SetPlayingArea(Rect value)
    {
        this._playingArea = value;
    }

    public Rect GetPlayerArea()
    {
        return this._playingArea;
    }

    public void SetProvision(bool value)
    {
        this._provisioned = value;
    }

    // integrate with LSF
    public void SaveDetails(JsonWriter writer)
    {
        SaveManager.WriteString(writer, "Type", objectName);
        SaveManager.WriteInt(writer, "GlobalID", GlobalID);
        SaveManager.WriteInt(writer, "LocalID", LocalID);
        SaveManager.WriteVector2d(writer, "Position", Body.Position);
        SaveManager.WriteVector2d(writer, "Rotation", Body.Rotation);
        SaveManager.WriteVector(writer, "Scale", transform.localScale);
    }

    public void LoadDetails(JsonTextReader reader)
    {
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)reader.Value;
                    reader.Read();
                    HandleLoadedProperty(reader, propertyName, reader.Value);
                }
            }
            else if (reader.TokenType == JsonToken.EndObject)
            {
                //loaded position invalidates the selection bounds so they must be recalculated
                Body.SetSelectionBounds(ResourceManager.InvalidBounds);
                Body.CalculateBounds();
                loadedSavedValues = true;
                return;
            }
        }
        //loaded position invalidates the selection bounds so they must be recalculated
        Body.SetSelectionBounds(ResourceManager.InvalidBounds);
        Body.CalculateBounds();
        loadedSavedValues = true;
    }

    public void SetTeamColor()
    {
        TeamColor[] teamColors = GetComponentsInChildren<TeamColor>();
        foreach (TeamColor teamColor in teamColors)
        {
            teamColor.GetComponent<Renderer>().material.color = cachedCommander.teamColor;
        }
    }

    public AgentCommander GetCommander()
    {
        return cachedCommander;
    }
    #endregion

    #region Private
    protected virtual void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
    {
        switch (propertyName)
        {
            case "Type":
                objectName = (string)readValue;
                break;
            case "GlobalID":
                this.GlobalID = (ushort)readValue;
                break;
            case "LocalID":
                this.LocalID = (ushort)readValue;
                break;
            case "Position":
                Body.Position = LoadManager.LoadVector2d(reader);
                break;
            case "Rotation":
                Body.Rotation = LoadManager.LoadVector2d(reader);
                break;
            case "Scale":
                transform.localScale = LoadManager.LoadVector(reader);
                break;
            default:
                break;
        }
    }
    #endregion
}



