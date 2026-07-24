#if MONO
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniverseLib;

namespace UniverseLib.Runtime.Mono
{
    internal class MonoProvider : RuntimeHelper
    {
        internal bool UseNewSceneHandle { get; private set; }

        internal FieldInfo? sceneField_Handle;
        internal MethodInfo? sceneHandleToInt;
        internal MethodInfo? intToSceneHandle;

        protected internal override void OnInitialize()
        {
            new MonoTextureHelper();
        }

        protected internal void Internal_SceneHandleInitialize()
        {
            Type? sceneType = ReflectionUtility.GetTypeByName("UnityEngine.SceneManagement.Scene");
            if (sceneType == null)
            {
                throw new Exception("This version of Unity does not ship with the 'Scene' class, or it was not unstripped.");
            }
            sceneField_Handle = AccessTools.Field(sceneType, "m_Handle");
            if (sceneField_Handle == null)
            {
                throw new Exception("This version of Unity does not ship with the 'Scene.m_Handle' field, or it was not unstripped.");
            }

            Type handleType = sceneField_Handle.FieldType;
            UseNewSceneHandle = handleType.FullName == "UnityEngine.SceneManagement.SceneHandle";
            if (!UseNewSceneHandle) return;

            sceneHandleToInt = AccessTools.GetDeclaredMethods(sceneType)
                    .FirstOrDefault(m =>
                        m.Name == "op_Implicit" &&
                        m.ReturnType == typeof(int) &&
                        m.GetParameters().FirstOrDefault()?.ParameterType == handleType
                    );
            MethodInfo? IntToSceneHandle = AccessTools.Method(sceneType, "op_Implicit", new Type[] { typeof(int) });
            if (sceneHandleToInt == null || IntToSceneHandle == null)
            {
                throw new Exception("This version of Unity does not ship with the 'SceneHandle' implicit conversion operators, or they were not unstripped.");
            }
        }

        /// <inheritdoc/>
        protected internal override Coroutine Internal_StartCoroutine(IEnumerator routine) 
            => UniversalBehaviour.Instance.StartCoroutine(routine);

        /// <inheritdoc/>
        protected internal override void Internal_StopCoroutine(Coroutine coroutine)
            => UniversalBehaviour.Instance.StopCoroutine(coroutine);

        /// <inheritdoc/>
        protected internal override T Internal_AddComponent<T>(GameObject obj, Type type) 
            => (T)obj.AddComponent(type);

        /// <inheritdoc/>
        protected internal override ScriptableObject Internal_CreateScriptable(Type type) 
            => ScriptableObject.CreateInstance(type);

        /// <inheritdoc/>
        protected internal override void Internal_GraphicRaycast(GraphicRaycaster raycaster, PointerEventData data, List<RaycastResult> list)
            => raycaster.Raycast(data, list);

        /// <inheritdoc/>
        protected internal override string Internal_LayerToName(int layer) 
            => LayerMask.LayerToName(layer);

        /// <inheritdoc/>
        protected internal override UnityEngine.Object[] Internal_FindObjectsOfTypeAll(Type type) 
            => Resources.FindObjectsOfTypeAll(type);

        protected internal override T[] Internal_FindObjectsOfTypeAll<T>()
            => Resources.FindObjectsOfTypeAll<T>();

        protected internal override int Internal_GetSceneIntHandle(Scene scene)
        {
            object? handle = sceneField_Handle.GetValue(scene);
            if (UseNewSceneHandle)
            {
                return (int)sceneHandleToInt.Invoke(null, new object[] { handle });
            }
            return (int)handle;
        }

        protected internal override Scene Internal_CreateSceneFromIntHandle(int sceneHandle)
        {
            Scene scene = new Scene();
            object handele = sceneHandle;
            if (UseNewSceneHandle)
            {
                handele = intToSceneHandle.Invoke(null, new object[] { sceneHandle });
            }
            sceneField_Handle.SetValue(scene, handele);
            return scene;
        }

        /// <inheritdoc/>
        protected internal override GameObject[] Internal_GetRootGameObjects(Scene scene) 
            => scene.isLoaded ? scene.GetRootGameObjects() : new GameObject[0];

        /// <inheritdoc/>
        protected internal override int Internal_GetRootCount(Scene scene) 
            => scene.rootCount;

        /// <inheritdoc/>
        protected internal override void Internal_SetColorBlock(Selectable selectable, ColorBlock colors)
            => selectable.colors = colors;

        /// <inheritdoc/>
        protected internal override void Internal_SetColorBlock(Selectable selectable, Color? normal = null, Color? highlighted = null, Color? pressed = null,
            Color? disabled = null)
        {
            ColorBlock colors = selectable.colors;

            if (normal != null)
                colors.normalColor = (Color)normal;

            if (highlighted != null)
                colors.highlightedColor = (Color)highlighted;

            if (pressed != null)
                colors.pressedColor = (Color)pressed;

            if (disabled != null)
                colors.disabledColor = (Color)disabled;

            Internal_SetColorBlock(selectable, colors);
        }
    }
}

public static class MonoExtensions
{
    // Helpers to use the same style of AddListener that IL2CPP uses.

    public static void AddListener(this UnityEvent _event, Action listener)
        => _event.AddListener(new UnityAction(listener));

    public static void AddListener<T>(this UnityEvent<T> _event, Action<T> listener)
        => _event.AddListener(new UnityAction<T>(listener));

    public static void RemoveListener(this UnityEvent _event, Action listener)
        => _event.RemoveListener(new UnityAction(listener));

    public static void RemoveListener<T>(this UnityEvent<T> _event, Action<T> listener)
        => _event.RemoveListener(new UnityAction<T>(listener));

    // Doesn't exist in NET 3.5

    public static void Clear(this StringBuilder sb) 
        => sb.Remove(0, sb.Length);

    // These properties don't exist in some earlier games, so null check before trying to set them.

    static PropertyInfo p_childControlHeight = AccessTools.Property(typeof(HorizontalOrVerticalLayoutGroup), "childControlHeight");
    static PropertyInfo p_childControlWidth = AccessTools.Property(typeof(HorizontalOrVerticalLayoutGroup), "childControlWidth");

    public static void SetChildControlHeight(this HorizontalOrVerticalLayoutGroup group, bool value)
        => p_childControlHeight?.SetValue(group, value, null);

    public static void SetChildControlWidth(this HorizontalOrVerticalLayoutGroup group, bool value)
        => p_childControlWidth?.SetValue(group, value, null);
}

#endif