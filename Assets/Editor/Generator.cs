using MiniExcelLibs;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;
using ZeroHero;

public class Generator
{
    #region Field
    private static string _defaultCfgPath = Application.dataPath + "/Config";
    private static string _cfgPath;
    private static string _sheetName;
    private static string _savePath;
    private static string _tableName;
    private static CodeCompileUnit unit;
    private static CodeNamespace sampleNamespace;
    private static CodeTypeDeclaration myClass;
    private static CodeMemberMethod codeMemberMethod;
    private static CodeDomProvider provider;
    private static CodeGeneratorOptions options;
    private static char[] splitChar = new char[1] { ':' };
    #endregion

    [MenuItem("Tools/Generate")]
    private static void GenerateCode()
    {
        InitDir();
        #region xml配置表生成工具

        unit = new CodeCompileUnit();
        myClass = new CodeTypeDeclaration("XmlGenerator");
        myClass.IsClass = true;
        myClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
        sampleNamespace = new CodeNamespace("ZeroHero");
        sampleNamespace.Imports.Add(new CodeNamespaceImport("MiniExcelLibs"));
        sampleNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
        sampleNamespace.Imports.Add(new CodeNamespaceImport("System.IO"));
        sampleNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
        sampleNamespace.Imports.Add(new CodeNamespaceImport("System.Xml.Serialization"));
        sampleNamespace.Imports.Add(new CodeNamespaceImport("UnityEditor"));
        sampleNamespace.Imports.Add(new CodeNamespaceImport("UnityEngine"));

        sampleNamespace.Types.Add(myClass);
        unit.Namespaces.Add(sampleNamespace);

        #region 添加字段
        ////1.
        CodeMemberField defaultCfgPath = new CodeMemberField(typeof(System.String), "defaultCfgPath");
        defaultCfgPath.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        defaultCfgPath.InitExpression = new CodePrimitiveExpression(_defaultCfgPath);
        myClass.Members.Add(defaultCfgPath);
        ////2.
        CodeMemberField cfgPath = new CodeMemberField(typeof(System.String), "cfgPath");
        cfgPath.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(cfgPath);
        ////3.
        CodeMemberField sheetName = new CodeMemberField(typeof(System.String), "sheetName");
        sheetName.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(sheetName);
        ////4.
        CodeMemberField tableName = new CodeMemberField(typeof(System.String), "tableName");
        tableName.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(tableName);
        ////5.
        CodeMemberField savePath = new CodeMemberField(typeof(System.String), "savePath");
        savePath.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(savePath);
        ////6.
        CodeMemberField dirInfo = new CodeMemberField(typeof(DirectoryInfo), "dirInfo");
        dirInfo.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(dirInfo);
        ////7.
        CodeMemberField fileInfos = new CodeMemberField(typeof(FileInfo[]), "fileInfos");
        fileInfos.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(fileInfos);
        ////8.
        CodeMemberField fileStream = new CodeMemberField(typeof(FileStream), "fileStream");
        fileStream.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(fileStream);
        ////9.
        CodeMemberField xmlFormatter = new CodeMemberField(typeof(XmlSerializer), "xmlFormatter");
        xmlFormatter.Attributes = MemberAttributes.Private | MemberAttributes.Static;
        myClass.Members.Add(xmlFormatter);
        #endregion

        #region 添加方法
        codeMemberMethod = new CodeMemberMethod();
        codeMemberMethod.Name = "SaveAllCfg2Xml";
        codeMemberMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
        codeMemberMethod.ReturnType = new CodeTypeReference(typeof(void));
        codeMemberMethod.Statements.Add(new CodeSnippetExpression(new StringBuilder().AppendFormat("Directory.CreateDirectory(\"{0}\")", Application.streamingAssetsPath + "/xml").ToString()));
        //遍历xlsx表
        DirectoryInfo Dir = new DirectoryInfo(Application.dataPath + "/Config");
        foreach (FileInfo info in Dir.GetFiles("*.xlsx", SearchOption.AllDirectories)) //查找文件
        {
            _cfgPath = info.FullName;
            _sheetName = MiniExcel.GetSheetNames(_cfgPath)[0];
            _tableName = Path.GetFileNameWithoutExtension(_cfgPath);
            _savePath = Application.streamingAssetsPath + "/xml/" + _tableName + ".xml";
            StringBuilder typeName = GetTypeNameBySheetName(_sheetName);
            StringBuilder listTypeName = GetTypeNameBySheetName(_sheetName);
            listTypeName.Insert(0, "List<");
            listTypeName.Append(">");
            StringBuilder listName = new StringBuilder(_sheetName.ToLower());
            listName.Append("List");
            StringBuilder dicName = new StringBuilder(_sheetName.ToLower());
            dicName.Append("Dictionary");

            //获取唯一标识的字段名、类型
            var columns = MiniExcel.GetColumns(_cfgPath, useHeaderRow: true, startCell: "A1");
            string propertyName = "";
            string propertyType = "";
            foreach (var col in columns)
            {
                string[] colSplited = col.Split(splitChar);
                if (colSplited.Length < 2)
                {
                    Debug.LogError(_tableName + "有字段未设置类型！");
                    return;
                }
                propertyName = colSplited[0];
                propertyType = colSplited[1];
                if (!utils.TYPE_REFLECTION.ContainsKey(propertyType))
                {
                    Debug.LogError(_sheetName + "字段类型'" + propertyType + "'不存在！");
                    return;
                }
                break;
            }
            StringBuilder dicType = new StringBuilder().AppendFormat("SerializableDictionary<{0}, {1}>", propertyType, typeName);



            codeMemberMethod.Statements.Add(new CodeSnippetExpression("tableName = " + "\"" + _tableName + "\""));
            codeMemberMethod.Statements.Add(new CodeAssignStatement(new CodeSnippetExpression("cfgPath"), new CodePrimitiveExpression(_cfgPath)));
            codeMemberMethod.Statements.Add(new CodeAssignStatement(new CodeSnippetExpression("savePath"), new CodePrimitiveExpression(_savePath)));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression(listTypeName + " " + listName + " = MiniExcel.Query<" + typeName + ">(cfgPath,\"" + _sheetName + "\",ExcelType.XLSX,\"A2\").ToList()"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression(dicType + " " + dicName + " = new " + dicType + "()"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("for (int i = 0; i < " + listName + ".Count; i++) {   " + typeName + " cfg = " + listName + "[i];  " + " if(cfg." + propertyName + "!=null)" + dicName + ".Add(cfg." + propertyName + ", cfg);}"));

            codeMemberMethod.Statements.Add(new CodeSnippetExpression("fileStream = new FileStream(savePath, FileMode.Create)"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("xmlFormatter = new XmlSerializer(typeof(" + dicType + "))"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("xmlFormatter.Serialize(fileStream, " + dicName + ")"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("fileStream.Close()"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("Debug.Log(savePath+\"-----生成\")"));
        }
        codeMemberMethod.Statements.Add(new CodeSnippetExpression("AssetDatabase.Refresh()"));
        codeMemberMethod.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(MenuItem)), new CodeAttributeArgument(new CodePrimitiveExpression("Tools/生成xml配置表"))));
        myClass.Members.Add(codeMemberMethod);

        #endregion 

        //添加构造器(使用CodeConstructor) --此处略
        //添加程序入口点（使用CodeEntryPointMethod） --此处略
        //添加事件（使用CodeMemberEvent) --此处略
        //添加特征(使用 CodeAttributeDeclaration)
        //Customerclass.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));
        #region 生成代码
        provider = CodeDomProvider.CreateProvider("CSharp");
        options = new CodeGeneratorOptions();
        options.BracingStyle = "CSharp";
        options.BlankLinesBetweenMembers = true;
        #endregion
        #region 输出文件
        string outputFile = Application.dataPath + "/Editor/XmlGenerator.cs";
        using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile))
        {
            provider.GenerateCodeFromCompileUnit(unit, sw, options);
        }
        #endregion
        #endregion
        #region excel配置表 To ***CfgMgr.cs
        //清理原来生成的CfgMgr
        string mgrPath = Application.dataPath + "/CSharp/Manager/CfgMgr";
        DirectoryInfo d = new DirectoryInfo(mgrPath);
        foreach (FileInfo item in d.GetFiles("*.cs", SearchOption.AllDirectories))
        {
            File.Delete(item.FullName);
        }
        //查找文件
        foreach (FileInfo info in Dir.GetFiles("*.xlsx", SearchOption.AllDirectories))
        {

            _cfgPath = info.FullName;
            _sheetName = MiniExcel.GetSheetNames(_cfgPath)[0];
            _tableName = Path.GetFileNameWithoutExtension(_cfgPath);

            string cfgType = _sheetName + "Cfg";
            string cfgListType = "List<" + cfgType + ">";
            string dicName = _sheetName.ToLower() + "CfgDic";

            unit = new CodeCompileUnit();
            sampleNamespace = new CodeNamespace("ZeroHero");
            sampleNamespace.Imports.Add(new CodeNamespaceImport("System"));
            sampleNamespace.Imports.Add(new CodeNamespaceImport("System.IO"));
            sampleNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sampleNamespace.Imports.Add(new CodeNamespaceImport("System.Xml.Serialization"));
            sampleNamespace.Imports.Add(new CodeNamespaceImport("UnityEngine"));
            sampleNamespace.Imports.Add(new CodeNamespaceImport("Unity.Mathematics"));
            sampleNamespace.Imports.Add(new CodeNamespaceImport("UnityEngine.Networking"));
            sampleNamespace.Imports.Add(new CodeNamespaceImport("System.Text"));

            #region [生成Cfg Class类]
            CodeTypeDeclaration cfgClass;
            cfgClass = new CodeTypeDeclaration(cfgType);
            cfgClass.IsClass = true;
            cfgClass.TypeAttributes = TypeAttributes.Public;
            cfgClass.CustomAttributes.Add(new CodeAttributeDeclaration("Serializable"));
            //获取唯一标识的字段名、类型
            var columns = MiniExcel.GetColumns(_cfgPath, useHeaderRow: true, startCell: "A1");
            string propertyName = "";
            string propertyType = "";
            string tagName = "";
            string tagType = "";
            foreach (var col in columns)
            {
                string[] colSplited = col.Split(splitChar);
                if (colSplited.Length < 2)
                {
                    Debug.LogError(_tableName + "有字段未设置类型！");
                    continue;
                }
                if (tagType == "")
                {
                    tagName = colSplited[0];
                    tagType = colSplited[1];
                }
                propertyName = colSplited[0];
                propertyType = colSplited[1];
                if (!utils.TYPE_REFLECTION.ContainsKey(propertyType))
                {
                    Debug.LogError(_sheetName + "字段类型'" + propertyType + "'不存在！");
                    continue;
                }
                var type = utils.TYPE_REFLECTION[propertyType]; ;
                //添加字段
                CodeMemberField field = new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = propertyName + "{ get; set; }//",
                    Type = new CodeTypeReference(type)
                };
                field.CustomAttributes.Add(
                    new CodeAttributeDeclaration(
                        new CodeTypeReference("XmlAttribute"),
                        new CodeAttributeArgument(new CodePrimitiveExpression(propertyName))
                    )
                );
                cfgClass.Members.Add(field);
            }
            StringBuilder dicType = new StringBuilder().AppendFormat("SerializableDictionary<{0}, {1}>", tagType, GetTypeNameBySheetName(_sheetName));
            sampleNamespace.Types.Add(cfgClass);
            #endregion

            #region [生成Cfg Struct类]
            CodeTypeDeclaration cfgStruct;
            cfgStruct = new CodeTypeDeclaration(_sheetName + "Struct");
            cfgStruct.IsStruct = true;
            cfgStruct.TypeAttributes = TypeAttributes.Public;
            //获取唯一标识的字段名、类型
            propertyName = "";
            propertyType = "";
            tagName = "";
            tagType = "";
            foreach (var col in columns)
            {
                string[] colSplited = col.Split(splitChar);
                if (colSplited.Length < 2)
                {
                    Debug.LogError(_tableName + "有字段未设置类型！");
                    continue;
                }
                if (tagType == "")
                {
                    tagName = colSplited[0];
                    tagType = colSplited[1];
                }
                propertyName = colSplited[0];
                propertyType = colSplited[1];
                if (!utils.TYPE_REFLECTION.ContainsKey(propertyType))
                {
                    Debug.LogError(_sheetName + "字段类型'" + propertyType + "'不存在！");
                    continue;
                }
                var type = utils.TYPE_REFLECTION[propertyType]; ;
                //添加字段
                CodeMemberField field = new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = propertyName,
                    Type = new CodeTypeReference(type)
                };
                cfgStruct.Members.Add(field);
            }
            sampleNamespace.Types.Add(cfgStruct);
            #endregion

            myClass = new CodeTypeDeclaration(_sheetName + "CfgMgr");
            myClass.IsClass = true;
            myClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
            sampleNamespace.Types.Add(myClass);

            unit.Namespaces.Add(sampleNamespace);

            #region 添加字段
            CodeMemberField cfgDic = new CodeMemberField(dicType.ToString(), dicName);
            cfgDic.Attributes = MemberAttributes.Private | MemberAttributes.Static;
            myClass.Members.Add(cfgDic);
            #endregion

            #region 添加方法
            codeMemberMethod = new CodeMemberMethod();
            codeMemberMethod.Name = "Init";
            codeMemberMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            codeMemberMethod.ReturnType = new CodeTypeReference(typeof(void));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("var uri = new System.Uri(Path.Combine(Application.streamingAssetsPath, \"xml\",\"" + _tableName + ".xml\"))"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("var request = UnityWebRequest.Get(uri.AbsoluteUri)"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("request.SendWebRequest()"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("while (!request.isDone){ if (request.isNetworkError) { Debug.Log(request.error); return;}}"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("byte[] bytes = Encoding.UTF8.GetBytes(request.downloadHandler.text)"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("MemoryStream stream = new MemoryStream()"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("stream.Write(bytes, 0, bytes.Length)"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("stream.Position = 0"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("XmlSerializer xmlFormatter = new XmlSerializer(typeof(" + dicType + "));"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression(dicName + " = (" + dicType + ")xmlFormatter.Deserialize(stream);"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("stream.Close();"));
            myClass.Members.Add(codeMemberMethod);

            codeMemberMethod = new CodeMemberMethod();
            codeMemberMethod.Name = "GetBy" + tagName;
            codeMemberMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            codeMemberMethod.ReturnType = new CodeTypeReference(cfgType);
            codeMemberMethod.Parameters.Add(new CodeParameterDeclarationExpression(utils.TYPE_REFLECTION[tagType], tagName.ToLower()));
            codeMemberMethod.Statements.Add(
                new CodeConditionStatement(
                    new CodeSnippetExpression("!" + dicName + ".TryGetValue(" + tagName.ToLower() + ", out " + cfgType + " cfg)"),
                    new CodeSnippetStatement[]
                    {
                        new CodeSnippetStatement("              Debug.LogError(\""+_tableName+": 配置表出错, 不存在id: \"+"+tagName.ToLower()+" );    "),
                        new CodeSnippetStatement("              return null;")
                    }
                )
            );
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("return cfg"));
            myClass.Members.Add(codeMemberMethod);

            codeMemberMethod = new CodeMemberMethod();
            codeMemberMethod.Name = "GetConfigList";
            codeMemberMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            codeMemberMethod.ReturnType = new CodeTypeReference(cfgListType);
            codeMemberMethod.Statements.Add(new CodeSnippetExpression(cfgListType + " list = new " + cfgListType + "()"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("foreach (var item in " + dicName + "){list.Add(item.Value);}"));
            codeMemberMethod.Statements.Add(new CodeSnippetExpression("return list"));
            myClass.Members.Add(codeMemberMethod);
            #endregion

            #region 生成代码
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "CSharp";
            options.BlankLinesBetweenMembers = true;
            #endregion
            #region 输出文件
            StringBuilder outPutName = new StringBuilder();
            outPutName.AppendFormat("{0}/CSharp/Manager/CfgMgr/{1}CfgMgr.cs", Application.dataPath, _sheetName);
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outPutName.ToString()))
            {
                provider.GenerateCodeFromCompileUnit(unit, sw, options);
            }
            Debug.Log(outPutName.Append("------生成"));
            #endregion

        }
        #endregion
    }

    #region Method
    private static void InitDir()
    {
        StringBuilder path = new StringBuilder(Application.dataPath + "/CSharp");
        if (!Directory.Exists(path.ToString())) Directory.CreateDirectory(path.ToString());
        path.Append("/Manager");
        if (!Directory.Exists(path.ToString())) Directory.CreateDirectory(path.ToString());
        path.Append("/CfgMgr");
        if (!Directory.Exists(path.ToString())) Directory.CreateDirectory(path.ToString());
    }
    #endregion
    private static StringBuilder GetTypeNameBySheetName(string sheetName)
    {
        StringBuilder typeName = new StringBuilder();
        typeName.Append(sheetName);
        typeName.Append("Cfg");
        return typeName;
    }

}
