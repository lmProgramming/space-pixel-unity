using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.ToolsExtensions
{
    public class UIToolkitMVCVMGenerator : EditorWindow
    {
        private string _namespaceName = "UI.Screens";
        private string _screenName = "NewScreen";

        private void OnGUI()
        {
            _screenName = EditorGUILayout.TextField(
                "Screen Name",
                _screenName);

            _namespaceName = EditorGUILayout.TextField(
                "Namespace",
                _namespaceName);

            GUILayout.Space(20);

            if (GUILayout.Button("Create")) Generate();
        }

        [MenuItem("Tools/UI Toolkit/Create MVCVM Screen")]
        private static void Open()
        {
            GetWindow<UIToolkitMVCVMGenerator>(
                "UI Screen Generator");
        }

        private void Generate()
        {
            var folder =
                $"Assets/Scripts/{_namespaceName.Replace(".", "/")}";

            Directory.CreateDirectory(folder);

            CreateUxml(folder);
            CreateUss(folder);

            CreateModel(folder);
            CreateViewModel(folder);
            CreateView(folder);
            CreateController(folder);

            AssetDatabase.Refresh();

            Debug.Log(
                $"Created UI Toolkit MVCVM screen: {_screenName}");
        }

        private static void CreateFile(
            string folder,
            string filename,
            string content)
        {
            File.WriteAllText(
                $"{folder}/{filename}",
                content);
        }

        private void CreateUxml(string folder)
        {
            CreateFile(
                folder,
                $"{_screenName}.uxml",
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<ui:UXML
        xmlns:ui=""UnityEngine.UIElements""
>
    <Style src=""project://database/Assets/DesignSystem/Resources/UI/Styles/DesignSystem/DesignSystem.uss?fileID=7433441132597879392&amp;guid=2fe121bab235f2a4f8cbc07737f87fe2&amp;type=3#DesignSystem""/>
    <ui:VisualElement class=""ds-root"">
    </ui:VisualElement>
</ui:UXML>");
        }

        private void CreateUss(string folder)
        {
            CreateFile(
                folder,
                $"{_screenName}.uss",
                @"
");
        }

        private void CreateModel(string folder)
        {
            CreateFile(
                folder,
                $"{_screenName}Model.cs",
                $@"using Core.MVCVM;

namespace {_namespaceName} 
{{
    public class {_screenName}Model : ObservableModel
    {{
    }}
}}
");
        }

        private void CreateViewModel(string folder)
        {
            CreateFile(
                folder,
                $"{_screenName}ViewModel.cs",
                $@"using UI.MVCVM;

namespace {_namespaceName} 
{{
    public class {_screenName}ViewModel
    {{
    }}
}}
");
        }


        private void CreateView(string folder)
        {
            CreateFile(
                folder,
                $"{_screenName}View.cs",
                $@"using UI.MVCVM;
using UnityEngine.UIElements;
using System;

namespace {_namespaceName} 
{{
    public class {_screenName}View 
        : View<{_screenName}ViewModel>
    {{
        public event Action CloseClicked;

        public override void BindUI(
            VisualElement root)
        {{
        }}

        public override void UnbindUI()
        {{
        }}

        public override void SetData(
            {_screenName}ViewModel viewModel)
        {{
        }}
    }}
}}
");
        }


        private void CreateController(string folder)
        {
            CreateFile(
                folder,
                $"{_screenName}Controller.cs",
                $@"using UI.MVCVM;

namespace {_namespaceName} 
{{
    public class {_screenName}Controller 
        : Controller<
            {_screenName}Model,
            {_screenName}View,
            {_screenName}ViewModel>
    {{
        protected override {_screenName}Model CreateModel()
        {{
            return new {_screenName}Model();
        }}

        protected override {_screenName}View CreateView()
        {{
            return new {_screenName}View();
        }}

        protected override {_screenName}ViewModel
            CreateViewModel(
                {_screenName}Model model)
        {{
            return new {_screenName}ViewModel();
        }}
    }}
}}
");
        }
    }
}