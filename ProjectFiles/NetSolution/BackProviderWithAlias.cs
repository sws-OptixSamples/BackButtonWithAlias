#region Using directives
using System;
using System.Collections.Generic;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using System.Linq;
#endregion

public class BackProviderWithAlias : BaseNetLogic
{
    public override void Start()
    {
        panelHistory = new Stack<PanelState>();

        var panelLoader = Owner as PanelLoader;
        if (panelLoader == null)
            Log.Error("BackProvider", "Panel loader not found");
        panelLoader.PanelVariable.VariableChange += PanelVariable_VariableChange;
    }

    private void PanelVariable_VariableChange(object sender, VariableChangeEventArgs e)
    {
        var oldPanel = InformationModel.Get(e.OldValue);
        if (oldPanel == null)
            return;

        var panelLoader = Owner as PanelLoader;
        NodeId oldPanelNodeId = e.OldValue;
        NodeId oldAliasNodeId = panelLoader.GetVariable("PreviousAliasId").Value;
        
        panelHistory.Push(new PanelState(oldPanelNodeId, oldAliasNodeId));
    }

    public override void Stop()
    {
        var panelLoader = Owner as PanelLoader;
        if (panelLoader == null)
            Log.Error("BackProvider", "Panel loader not found");

        panelLoader.PanelVariable.VariableChange -= PanelVariable_VariableChange;
    }

    [ExportMethod]
    public void Back()
    {
        var panelLoader = Owner as PanelLoader;
        if (panelLoader == null)
            Log.Error("BackProvider", "Panel loader not found");

        if (panelHistory.Count == 0)
            return;

        var previousState = panelHistory.Pop();
        panelLoader.PanelVariable.VariableChange -= PanelVariable_VariableChange;
        panelLoader.ChangePanel(previousState.PanelNodeId, previousState.AliasNodeId);
        panelLoader.PanelVariable.VariableChange += PanelVariable_VariableChange;
    }

    private Stack<PanelState> panelHistory;

    private class PanelState
    {
        public NodeId PanelNodeId { get; }
        public NodeId AliasNodeId { get; }

        public PanelState(NodeId panelNodeId, NodeId aliasNodeId)
        {
            PanelNodeId = panelNodeId;
            AliasNodeId = aliasNodeId;
        }
    }

    [ExportMethod]
    public void LoadPreviousAliasId()
    {
        Log.Info(Project.Current.Get("Model/DataObjects/AxisObjects/Axis1").NodeId.ToString());

        var panelLoader = Owner as PanelLoader;
        if (panelLoader == null)
            Log.Error("BackProvider", "Panel loader not found");
        var panel = panelLoader.Children.ElementAt(0);
        if (panel == null)
            return;
        var aliasNodeId = (NodeId)panel.GetVariable("Alias").Value;
        if (aliasNodeId != null)
        {
            var browseName = InformationModel.Get(aliasNodeId).BrowseName;
            Log.Info("BackProvider", $"Current Alias: {browseName} - {aliasNodeId.ToString()}");
            panelLoader.GetVariable("PreviousAliasId").Value = aliasNodeId;

        }
        else
        {
            Log.Info("BackProvider", "No Alias variable found for the current panel");
        }
    }
}
