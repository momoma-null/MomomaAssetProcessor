﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityObject = UnityEngine.Object;

#nullable enable

namespace MomomaAssets.GraphView
{
    using GraphView = UnityEditor.Experimental.GraphView.GraphView;

    [Serializable]
    sealed class NodeData : INodeData, IAdditionalAssetHolder, ISerializationCallbackReceiver
    {
        [SerializeField]
        bool m_Expanded = true;
        [SerializeField]
        List<PortData> m_InputPorts = new List<PortData>();
        [SerializeField]
        List<PortData> m_OutputPorts = new List<PortData>();
        [SerializeReference]
        INodeProcessor m_Processor;

        [NonSerialized]
        NodeDataEditor? m_NodeDataEditor;

        public string GraphElementName => m_Processor.GetType().Name;
        public int Priority => 0;
        public IGraphElementEditor GraphElementEditor => m_NodeDataEditor ?? (m_NodeDataEditor = new NodeDataEditor(m_Processor.ProcessorEditor));
        public IEnumerable<UnityObject> Assets => m_Processor is IAdditionalAssetHolder assetHolder ? assetHolder.Assets : Array.Empty<UnityObject>();
        public bool Expanded => m_Expanded;
        public INodeProcessor Processor => m_Processor;
        public IList<PortData> InputPorts => m_InputPorts;
        public IList<PortData> OutputPorts => m_OutputPorts;

        public NodeData(INodeProcessor processor)
        {
            m_Processor = processor;
            m_Processor.Initialize(this);
        }

        public GraphElement Deserialize() => new BindableNode(this);

        public void OnClone()
        {
            if (m_Processor is IAdditionalAssetHolder assetHolder)
                assetHolder.OnClone();
        }

        public void SetPosition(GraphElement graphElement, Rect position) => graphElement.SetPosition(position);

        public void DeserializeOverwrite(GraphElement graphElement, GraphView graphView)
        {
            if (!(graphElement is Node node))
                throw new InvalidOperationException();
            var toDeleteElements = new HashSet<GraphElement>();
            var ports = node.inputContainer.Query<Port>().ToList().ToDictionary(i => i.viewDataKey, i => i);
            var portCount = 0;
            foreach (var data in InputPorts)
            {
                if (ports.TryGetValue(data.Id, out var port))
                {
                    ports.Remove(data.Id);
                    if (port.portType != data.PortType)
                    {
                        port.portName = "";
                        port.portType = data.PortType;
                        foreach (var edge in port.connections)
                            if (!port.portType.IsAssignableFrom(edge.output.portType))
                                toDeleteElements.Add(edge);
                    }
                }
                else
                {
                    port = new Port<BindableEdge>(Orientation.Horizontal, Direction.Input, data.IsMulti ? Port.Capacity.Multi : Port.Capacity.Single, data.PortType, new EdgeConnectorListener());
                    if (!string.IsNullOrEmpty(data.Id))
                        port.viewDataKey = data.Id;
                }
                if (!string.IsNullOrEmpty(data.PortName))
                    port.portName = data.PortName;
                node.inputContainer.Insert(portCount++, port);
            }
            foreach (var port in ports.Values)
            {
                toDeleteElements.UnionWith(port.connections);
                node.inputContainer.Remove(port);
            }
            ports = node.outputContainer.Query<Port>().ToList().ToDictionary(i => i.viewDataKey, i => i);
            portCount = 0;
            foreach (var data in OutputPorts)
            {
                if (ports.TryGetValue(data.Id, out var port))
                {
                    ports.Remove(data.Id);
                    if (port.portType != data.PortType)
                    {
                        port.portName = "";
                        port.portType = data.PortType;
                        foreach (var edge in port.connections)
                            if (!edge.input.portType.IsAssignableFrom(port.portType))
                                toDeleteElements.Add(edge);
                    }
                }
                else
                {
                    port = new Port<BindableEdge>(Orientation.Horizontal, Direction.Output, data.IsMulti ? Port.Capacity.Multi : Port.Capacity.Single, data.PortType, new EdgeConnectorListener());
                    if (!string.IsNullOrEmpty(data.Id))
                        port.viewDataKey = data.Id;
                }
                if (!string.IsNullOrEmpty(data.PortName))
                    port.portName = data.PortName;
                node.outputContainer.Insert(portCount++, port);
            }
            foreach (var port in ports.Values)
            {
                toDeleteElements.UnionWith(port.connections);
                node.outputContainer.Remove(port);
            }
            node.extensionContainer.Query<IMGUIContainer>().ForEach(i => i.MarkDirtyLayout());
            node.extensionContainer.Query<PropertyField>().ForEach(i => i.MarkDirtyRepaint());
            if (node.expanded != true)
                node.expanded = true;
            else
                node.RefreshExpandedState();
            node.schedule.Execute(() => node.expanded = m_Expanded).ExecuteLater(1);
            graphView.DeleteElements(toDeleteElements);
        }

        public void ReplaceGuid(Dictionary<string, string> guids)
        {
            foreach (var i in InputPorts)
            {
                if (!guids.TryGetValue(i.Id, out var newGuid))
                {
                    newGuid = PortData.GetNewId();
                    guids.Add(i.Id, newGuid);
                }
                i.Id = newGuid;
            }
            foreach (var i in OutputPorts)
            {
                if (!guids.TryGetValue(i.Id, out var newGuid))
                {
                    newGuid = PortData.GetNewId();
                    guids.Add(i.Id, newGuid);
                }
                i.Id = newGuid;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            var ports = new HashSet<string>();
            for (var i = 0; i < m_InputPorts.Count; ++i)
            {
                if (!ports.Add(m_InputPorts[i].Id))
                {
                    m_InputPorts[i].Id = PortData.GetNewId();
                    ports.Add(m_InputPorts[i].Id);
                }
            }
            ports.Clear();
            for (var i = 0; i < m_OutputPorts.Count; ++i)
            {
                if (!ports.Add(m_OutputPorts[i].Id))
                {
                    m_OutputPorts[i].Id = PortData.GetNewId();
                    ports.Add(m_OutputPorts[i].Id);
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        sealed class NodeDataEditor : IGraphElementEditor
        {
            INodeProcessorEditor m_ProcessorEditor;

            public NodeDataEditor(INodeProcessorEditor processorEditor)
            {
                m_ProcessorEditor = processorEditor;
            }

            public bool UseDefaultVisualElement => m_ProcessorEditor.UseDefaultVisualElement;

            public void OnEnable() => m_ProcessorEditor.OnEnable();
            public void OnDisable() => m_ProcessorEditor.OnDisable();

            public void OnGUI(SerializedProperty property)
            {
                using (var m_ProcessorProperty = property.FindPropertyRelative(nameof(m_Processor)))
                using (var m_InputPortsProperty = property.FindPropertyRelative(nameof(m_InputPorts)))
                using (var m_OutputPortsProperty = property.FindPropertyRelative(nameof(m_OutputPorts)))
                {
                    m_ProcessorEditor.OnGUI(m_ProcessorProperty, m_InputPortsProperty, m_OutputPortsProperty);
                }
            }
        }
    }
}