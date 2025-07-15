using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Pastime.Stats.Editor {
    [CustomPropertyDrawer(typeof(StatContainer<>))]
    public class StatContainerPropertyDrawer : PropertyDrawer {
        private class StatData {
            public string Name { get; set; }
            public float InitialValue { get; set; }
            public float BaseValue { get; set; }
            public float CurrentValue { get; set; }
        }
        
        private const string STYLE_SHEET_NAME = "StatContainerPropertyStyleSheet";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var container = new VisualElement {
                name = "StatContainerPropertyDrawer"
            };

            var styleSheet = GetStyleSheet();
            if(!styleSheet) {
                Debug.LogError($"StyleSheet '{STYLE_SHEET_NAME}' could not be loaded. Ensure it exists in the project.");
                return container;
            }
            
            container.styleSheets.Add(styleSheet);

            var statsList = property.FindPropertyRelative("statsList");
            var propertyName = property.displayName;;
            
            if (statsList == null) return container;
            var multiColumnList = CreateMultiColumnList(statsList, propertyName);
            container.Add(multiColumnList);
            return container;
        }
        
        private StyleSheet GetStyleSheet() {
            var guid = AssetDatabase.FindAssets($"{STYLE_SHEET_NAME} t:StyleSheet").FirstOrDefault();
            if (string.IsNullOrEmpty(guid)) {
                Debug.LogError($"StyleSheet '{STYLE_SHEET_NAME}' not found. Please ensure it exists in the project.");
                return null;
            }
            
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (styleSheet) return styleSheet;
            
            Debug.LogError($"Failed to load StyleSheet at path: {path}");
            return null;
        }
        
        private MultiColumnListView CreateMultiColumnList(SerializedProperty statsList, string propertyName) {
            var statsData = new List<StatData>();
            for (int i = 0; i < statsList.arraySize; i++) {
                var statProperty = statsList.GetArrayElementAtIndex(i);
                var statData = new StatData {
                    Name = GetStatName(statProperty, i),
                    InitialValue = GetInitialValue(statProperty),
                    BaseValue = GetBaseValue(statProperty),
                    CurrentValue = GetCurrentValue(statProperty)
                };
                statsData.Add(statData);
            }
            
            var columns = new List<Column> {
                new() {
                    name = "StatName",
                    title = "Stat Name",
                    stretchable = true,
                    resizable = true,
                    makeCell = () => new Label(),
                    bindCell = (element, index) => {
                        var label = (Label)element;
                        label.AddToClassList("stat-label");
                        label.text = statsData[index].Name;
                    }
                },
                new() {
                    name = "InitialValue",
                    title = "Initial Value",
                    stretchable = true,
                    resizable = true,
                    makeCell = () => new Label(),
                    bindCell = (element, index) => {
                        var label = (Label)element;
                        label.AddToClassList("stat-label");
                        label.text = statsData[index].InitialValue.ToString("F1");
                    }
                    
                },
                new() {
                    name = "BaseValue",
                    title = "Base Value",
                    stretchable = true,
                    resizable = true,
                    makeCell = () => new Label(),
                    bindCell = (element, index) => {
                        var label = (Label)element;
                        label.AddToClassList("stat-label");
                        label.text = statsData[index].BaseValue.ToString("F1");
                    }
                },
                new() {
                    name = "CurrentValue",
                    title = "Current Value",
                    stretchable = true,
                    resizable = true,
                    makeCell = () => new Label(),
                    bindCell = (element, index) => {
                        var label = (Label)element;
                        label.AddToClassList("stat-label");
                        label.text = statsData[index].CurrentValue.ToString("F1");
                    }
                }
            };
            
            var multiColumnListView = new MultiColumnListView {
                itemsSource = statsData,
                fixedItemHeight = 20,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showFoldoutHeader = true,
                headerTitle = propertyName,
                showBoundCollectionSize = false
            };
            
            foreach (var column in columns) {
                multiColumnListView.columns.Add(column);
            }
            
            return multiColumnListView;
        }
        
        private string GetStatName(SerializedProperty statProperty, int index) {
            var enumType = GetEnumTypeFromContainer(statProperty);
            if (enumType == null) return $"Stat {index}";
            var enumValues = Enum.GetValues(enumType);
            return index < enumValues.Length ? enumValues.GetValue(index).ToString().ToLower() : $"Stat {index}";
        }
        
        private Type GetEnumTypeFromContainer(SerializedProperty statProperty) {
            try {
                var containerProperty = statProperty.serializedObject.FindProperty(statProperty.propertyPath.Split('.')[0]);
                var target = containerProperty.serializedObject.targetObject;
                var info = target.GetType().GetField(containerProperty.name, 
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                
                if (info != null) {
                    var genericType = info.FieldType.GetGenericArguments().FirstOrDefault();
                    if (genericType != null && genericType.IsEnum) {
                        return genericType;
                    }
                }
            }
            catch {
                Debug.LogError($"Failed to get enum type from property: {statProperty.propertyPath}. " +
                               "Ensure the property is part of a valid stat container.");
            }
            return null;
        }
        
        private float GetBaseValue(SerializedProperty statProperty) {
            var baseValueProp = statProperty.FindPropertyRelative("baseValue");
            return baseValueProp?.floatValue ?? 0f;
        }
        
        private float GetInitialValue(SerializedProperty statProperty) {
            var initialValueProp = statProperty.FindPropertyRelative("initialValue");
            return initialValueProp?.floatValue ?? GetBaseValue(statProperty);
        }
        
        private float GetCurrentValue(SerializedProperty statProperty) {
            var currentValueProp = statProperty.FindPropertyRelative("currentValue");
            return currentValueProp?.floatValue ?? GetBaseValue(statProperty);
        }
    }
    
   
}