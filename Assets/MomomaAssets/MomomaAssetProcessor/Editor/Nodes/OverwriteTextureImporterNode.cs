using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityObject = UnityEngine.Object;
using static UnityEngine.Object;

#nullable enable

namespace MomomaAssets.GraphView.AssetProcessor
{
    [InitializeOnLoad]
    [Serializable]
    sealed class OverwriteTextureImporterNode : INodeData, IAdditionalAssetHolder
    {
        sealed class OverwriteTextureImporterNodeEditor : IGraphElementEditor
        {
            Editor? m_CachedEditor;

            public bool UseDefaultVisualElement => false;

            public void OnDestroy()
            {
                DestroyImmediate(m_CachedEditor);
            }

            public void OnGUI(SerializedProperty property)
            {
                using (var m_ImporterProperty = property.FindPropertyRelative(nameof(m_Importer)))
                {
                    if (m_ImporterProperty.objectReferenceValue == null)
                        return;
                    Editor.CreateCachedEditor(m_ImporterProperty.objectReferenceValue, null, ref m_CachedEditor);
                    m_CachedEditor.OnInspectorGUI();
                }
            }
        }

        sealed class AssetData
        {
            public static readonly TextureImporter s_DefaultImporter = Resources.Load<TextureImporter>("TextureImporter");
        }

        static OverwriteTextureImporterNode()
        {
            INodeDataUtility.AddConstructor(() => new OverwriteTextureImporterNode());
        }

        OverwriteTextureImporterNode() { }

        public IGraphElementEditor GraphElementEditor { get; } = new OverwriteTextureImporterNodeEditor();
        public string MenuPath => "Importer/Texture";
        public IEnumerable<PortData> InputPorts => new[] { m_InputPort };
        public IEnumerable<PortData> OutputPorts => new[] { m_OutputPort };
        public IEnumerable<UnityObject> Assets
        {
            get
            {
                if (m_Importer == null)
                {
                    m_Importer = Instantiate(AssetData.s_DefaultImporter);
                    m_Importer.name = m_Importer.name.Replace("(Clone)", "");
                }
                return new[] { m_Importer };
            }
        }

        [SerializeField]
        [HideInInspector]
        PortData m_InputPort = new PortData(typeof(Texture));

        [SerializeField]
        [HideInInspector]
        PortData m_OutputPort = new PortData(typeof(Texture));

        [SerializeField]
        TextureImporter? m_Importer = null;

        public void Process(ProcessingDataContainer container)
        {
            var assetGroup = container.Get(m_InputPort.Id, () => new AssetGroup());
            if (m_Importer != null)
            {
                foreach (var assets in assetGroup)
                {
                    var path = assets.AssetPath;
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                    {
                        using (var srcSO = new SerializedObject(m_Importer))
                        using (var iterotor = srcSO.GetIterator())
                        using (var dstSO = new SerializedObject(importer))
                        {
                            iterotor.Next(true);
                            var excludePaths = new HashSet<string>() { "m_Name", "m_UsedFileIDs", "m_Output" };
                            while (true)
                            {
                                if (!excludePaths.Contains(iterotor.propertyPath))
                                    dstSO.CopyFromSerializedPropertyIfDifferent(iterotor);
                                if (!iterotor.Next(false))
                                    break;
                            }
                            if (dstSO.hasModifiedProperties)
                            {
                                dstSO.ApplyModifiedPropertiesWithoutUndo();
                                AssetDatabase.WriteImportSettingsIfDirty(path);
                                importer.SaveAndReimport();
                            }
                        }
                    }
                }
            }
            container.Set(m_OutputPort.Id, assetGroup);
        }
    }
}