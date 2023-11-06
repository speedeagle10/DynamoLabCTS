using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dynamo.Graph.Nodes;
using RevitServices.Transactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamoLab.Revit.Elements
{
    /// <summary>
    /// Utility class that contains methods of Revit Parameters.
    /// </summary>
    public class Parameter
    {
        private Parameter() { }

        #region "Shared Parameters"
        //https://forums.autodesk.com/t5/revit-api-forum/adding-a-shared-parameter/td-p/6015818
        //https://forums.autodesk.com/t5/revit-api-forum/shared-parameter-as-project-parameter/td-p/6833022
        //https://forums.autodesk.com/t5/revit-api-forum/revit-2022-parametertype-text-to-forgetypeid/m-p/10231439#M55099
        /// <summary>
        /// Add project shared parameters and assign to single category,this methods works for Revit 2021 and later versions.
        /// From SDK worked example DoorSwing: DoorSharedParameter.CS
        /// </summary>
        /// <param name="paraName">New defined shared Parameter Name.</param>
        /// <param name="groupName">The shared parameter group which is defined at Edit Shared Parameters dialog.</param>
        /// <param name="paraType">Type of Parameter choosen from Parameter Properties dialog, for example text or int or YesNo.</param>
        /// <param name="dynamoCategory">Category of the shared parameter applied to, for example Structural Columns, Doors.</param>
        /// <param name="builtInParaGroupName">Information from Group Parameter Under,built-in parameter groups supported by Autodesk Revit, for example PG_TEXT, PG_IDENTITY_DATA.</param>
        /// <param name="instanceOrType">ture means family instance parameter, false means family type parameter.</param>
        [NodeCategory("Create")]  //works because we addusing Dynamo.Graph.Nodes; Reference from DynamoVisualProgramming.Core at NuGet Package
        public static void AddSharedParameters_1Category_ZTN(string paraName, string groupName, string paraType,
            global::Revit.Elements.Category dynamoCategory, string builtInParaGroupName, bool instanceOrType = true)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            UIApplication uiapp = RevitServices.Persistence.DocumentManager.Instance.CurrentUIApplication;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            // Create a new Binding object with the categories to which the parameter will be bound.
            CategorySet categories = uiapp.Application.Create.NewCategorySet();

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories01 = document.Settings.Categories;
            Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)revitCategory.Id.IntegerValue;

            // get category and insert into the CategorySet. Apply the shared parameters to multiple categories
            categories.Insert(revitCategory);

            // Open the shared parameters file 
            // via the private method AccessOrCreateSharedParameterFile
            DefinitionFile defFile = AccessOrCreateSharedParameterFile(uiapp.Application);
            if (null == defFile)
            {
                return;
            }

            // Access an existing or create a new group in the shared parameters file
            DefinitionGroups defGroups = defFile.Groups;
            DefinitionGroup defGroup = defGroups.get_Item(groupName);
            if (null == defGroup)
            {
                defGroup = defGroups.Create(groupName);
            }


            // Access an existing or create a new external parameter definition belongs to a specific group.
            //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
            // for "newSharedPara"
            if (!AlreadyAddedSharedParameter(document, paraName, builtInCategory))
            {
                Definition newSharedPara = defGroup.Definitions.get_Item(paraName);

                if (null == newSharedPara)
                {
                    //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
                    ForgeTypeId forgeTypeId = ForgeTypeIdByParameterTypeName_ZTN(paraType);
                    ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(paraName, forgeTypeId);
                    //SpecTypeId.String.Text
                    newSharedPara = defGroup.Definitions.Create(ExternalDefinitionCreationOptions);
                }

                // create one instance binding if true; and one type binding if false
                //Use an InstanceBinding or a TypeBinding object to create a new Binding object
                //that includes the categories to which the parameter is bound
                BuiltInParameterGroup builtInParaGroup = BuiltInParameterGroupAccordingToName_ZTN(builtInParaGroupName);
                if (instanceOrType)
                {
                    //Parameter binding connects a parameter definition to elements within one or more categories
                    InstanceBinding paraBinding = uiapp.Application.Create.NewInstanceBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    Autodesk.Revit.DB.BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }
                else
                {
                    TypeBinding paraBinding = uiapp.Application.Create.NewTypeBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }

            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

        }


        /// <summary>
        /// Add shared parameters needed in this sample, ,this methods works for Revit 2021 and later versions.
        /// From SDK worked example DoorSwing: DoorSharedParameter.CS
        /// </summary>
        /// <param name="paraName">New defined shared Parameter Name.</param>
        /// <param name="groupName">The shared parameter group which is defined at Edit Shared Parameters dialog.</param>
        /// <param name="paraType">Type of Parameter choosen from Parameter Properties dialog, for example text or int or YesNo.</param>
        /// <param name="dynamoCategoryList">Category of the shared parameter applied to, for example Structural Columns, Doors.</param>
        /// <param name="builtInParaGroupName">Information from Group Parameter Under,built-in parameter groups supported by Autodesk Revit, for example PG_TEXT, PG_IDENTITY_DATA.</param>
        /// <param name="instanceOrType">ture means family instance parameter, false means family type parameter.</param>
        [NodeCategory("Create")]
        public static void AddSharedParametersToCategoryList_ZTN(string paraName, string groupName, string paraType,
            List<global::Revit.Elements.Category> dynamoCategoryList, string builtInParaGroupName, bool instanceOrType = true)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            UIApplication uiapp = RevitServices.Persistence.DocumentManager.Instance.CurrentUIApplication;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            // Open the shared parameters file 
            // via the private method AccessOrCreateSharedParameterFile
            DefinitionFile defFile = AccessOrCreateSharedParameterFile(uiapp.Application);
            if (null == defFile)
            {
                return;
            }

            // Access an existing or create a new group in the shared parameters file
            DefinitionGroups defGroups = defFile.Groups;
            DefinitionGroup defGroup = defGroups.get_Item(groupName);
            if (null == defGroup)
            {
                defGroup = defGroups.Create(groupName);
            }

            // Create a new Binding object with the categories to which the parameter will be bound.
            CategorySet categories = uiapp.Application.Create.NewCategorySet();
            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories01 = document.Settings.Categories;

            foreach (global::Revit.Elements.Category dynamoCategory in dynamoCategoryList)
            {
                Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
                // get category and insert into the CategorySet. Apply the shared parameters to multiple categories
                categories.Insert(revitCategory);
            }

            foreach (global::Revit.Elements.Category dynamoCategory in dynamoCategoryList)
            {
                Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
                BuiltInCategory builtInCategory = (BuiltInCategory)revitCategory.Id.IntegerValue;

                //如果Paraname已存在，但是不存在于builtInCategory里，仅勾选category
                //如果Paraname不存在，创建参数并勾选category
                // create one instance binding if true; and one type binding if false
                //Use an InstanceBinding or a TypeBinding object to create a new Binding object
                //that includes the categories to which the parameter is bound                
                //Parameter binding connects a parameter definition to elements within one or more categories
                InstanceBinding instanceBinding = uiapp.Application.Create.NewInstanceBinding(categories);
                TypeBinding typeBinding = uiapp.Application.Create.NewTypeBinding(categories);

                //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                Autodesk.Revit.DB.BindingMap bindingMap = document.ParameterBindings;

                // Access an existing or create a new external parameter definition belongs to a specific group.
                //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
                // for "newSharedPara"      问题估计出在这里
                //如果Paraname已存在，builtInCategory已勾选，不做任何操作
                if (!AlreadyAddedSharedParameter(document, paraName, builtInCategory))
                {
                    Definition newSharedPara = defGroup.Definitions.get_Item(paraName);

                    //Create new shared parameter if not exist in the .txt file
                    if (null == newSharedPara)
                    {
                        //https://www.revitapidocs.com/2023/449e1cdb-ae48-6474-4da5-979c14b408f8.htm
                        ForgeTypeId forgeTypeId = ForgeTypeIdByParameterTypeName_ZTN(paraType);
                        ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(paraName, forgeTypeId);
                        //create new shared parameter
                        newSharedPara = defGroup.Definitions.Create(ExternalDefinitionCreationOptions);
                    }

                    BuiltInParameterGroup builtInParaGroup = BuiltInParameterGroupAccordingToName_ZTN(builtInParaGroupName);
                    //https://www.revitapidocs.com/2022/c3bed87a-956f-47c3-060c-0294c7ef43e7.htm
                    if (instanceOrType)
                    {
                        // Add the binding and definition to the document.
                        bindingMap.Insert(newSharedPara, instanceBinding, builtInParaGroup);
                    }
                    else
                    {
                        // Add the binding and definition to the document.
                        bindingMap.Insert(newSharedPara, typeBinding, builtInParaGroup);
                    }
                }

            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();
        }


        /*
        //https://forums.autodesk.com/t5/revit-api-forum/adding-a-shared-parameter/td-p/6015818
        //https://forums.autodesk.com/t5/revit-api-forum/shared-parameter-as-project-parameter/td-p/6833022
        //https://forums.autodesk.com/t5/revit-api-forum/revit-2022-parametertype-text-to-forgetypeid/m-p/10231439#M55099           
        /// <summary>
        /// Add shared parameters needed in this sample, this methods works for Revit 2022 and former versions.
        /// </summary>
        /// <param name="paraName">New defined shared Parameter Name.</param>
        /// <param name="groupName">The shared parameter group which is defined at Edit Shared Parameters dialog.</param>
        /// <param name="paraType">Type of Parameter choosen from Parameter Properties dialog.</param>
        /// <param name="dynamoCategory">Category of the shared parameter applied to.</param>
        /// <param name="builtInParaGroup">Information from Group Parameter Under.</param>
        /// <param name="instanceOrType">ture means family instance parameter, false means family type parameter.</param>
        public static void AddSharedParameters22_ZTN(string paraName, string groupName, ParameterType paraType,
            Revit.Elements.Category dynamoCategory, BuiltInParameterGroup builtInParaGroup, bool instanceOrType)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            UIApplication uiapp = RevitServices.Persistence.DocumentManager.Instance.CurrentUIApplication;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            // Create a new Binding object with the categories to which the parameter will be bound.
            CategorySet categories = uiapp.Application.Create.NewCategorySet();

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories01 = document.Settings.Categories;
            Autodesk.Revit.DB.Category revitCategory = categories01.get_Item(dynamoCategory.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)revitCategory.Id.IntegerValue;

            // get category and insert into the CategorySet.
            categories.Insert(revitCategory);

            // Open the shared parameters file 
            // via the private method AccessOrCreateSharedParameterFile
            DefinitionFile defFile = AccessOrCreateSharedParameterFile(uiapp.Application);
            if (null == defFile)
            {
                return;
            }

            // Access an existing or create a new group in the shared parameters file
            DefinitionGroups defGroups = defFile.Groups;
            DefinitionGroup defGroup = defGroups.get_Item(groupName);

            if (null == defGroup)
            {
                defGroup = defGroups.Create(groupName);
            }


            // Access an existing or create a new external parameter definition belongs to a specific group.
            //https://www.revitapidocs.com/2022/e31e22aa-179f-322b-7ae5-f3f840cf4158.htm

            // for "newSharedPara"
            if (!AlreadyAddedSharedParameter(document, paraName, builtInCategory))
            {
                Definition newSharedPara = defGroup.Definitions.get_Item(paraName);

                if (null == newSharedPara)
                {
                    //https://www.revitapidocs.com/2022/e31e22aa-179f-322b-7ae5-f3f840cf4158.htm
                    ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(paraName, paraType);
                    //SpecTypeId.String.Text
                    newSharedPara = defGroup.Definitions.Create(ExternalDefinitionCreationOptions);
                }

                // create one instance binding if true;
                // and one type binding if false
                if (instanceOrType)
                {
                    InstanceBinding paraBinding = uiapp.Application.Create.NewInstanceBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    Autodesk.Revit.DB.BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }
                else
                {
                    TypeBinding paraBinding = uiapp.Application.Create.NewTypeBinding(categories);
                    //BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
                    BindingMap bindingMap = document.ParameterBindings;
                    // Add the binding and definition to the document.
                    bindingMap.Insert(newSharedPara, paraBinding, builtInParaGroup);
                }

            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

        }
        */

        /// <summary>
        /// Access an existing or create a new shared parameters file.
        /// </summary>
        /// <param name="app">Revit Application.</param>
        /// <returns>the shared parameters file.</returns>
        private static DefinitionFile AccessOrCreateSharedParameterFile(Autodesk.Revit.ApplicationServices.Application app)
        {
            // The location of this command assembly
            string currentCommandAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // The path of ourselves shared parameters file
            string sharedParameterFilePath = Path.GetDirectoryName(currentCommandAssemblyPath);
            sharedParameterFilePath += "\\Shared Parameter.txt";

            // Check if the file exits
            System.IO.FileInfo documentMessage = new FileInfo(sharedParameterFilePath);
            bool fileExist = documentMessage.Exists;

            // Create file for external shared parameter since it does not exist
            if (!fileExist)
            {
                FileStream fileFlow = File.Create(sharedParameterFilePath);
                fileFlow.Close();
            }

            // Set ourselves file to the externalSharedParameterFile 
            app.SharedParametersFilename = sharedParameterFilePath;
            //Method's return
            DefinitionFile sharedParameterFile = app.OpenSharedParameterFile();

            return sharedParameterFile;
        }

        /// <summary>
        /// Has the specific document shared parameter already been added ago?
        /// </summary>
        /// <param name="doc">Revit project in which the shared parameter will be added.</param>
        /// <param name="paraName">the name of the shared parameter.</param>
        /// <param name="boundCategory">Which category the parameter will bind to</param>
        /// <returns>Returns true if already added ago else returns false.</returns>
        private static bool AlreadyAddedSharedParameter(Document doc, string paraName, BuiltInCategory boundCategory)
        {
            try
            {
                BindingMap bindingMap = doc.ParameterBindings;
                DefinitionBindingMapIterator bindingMapIter = bindingMap.ForwardIterator();

                while (bindingMapIter.MoveNext())
                {
                    //find the parameter according to name
                    if (bindingMapIter.Key.Name.Equals(paraName))
                    {
                        //find all checked category related to this shared parameter
                        ElementBinding binding = bindingMapIter.Current as ElementBinding;
                        CategorySet categories = binding.Categories;

                        foreach (Autodesk.Revit.DB.Category category in categories)
                        {
                            //这里一种特殊情况：ParaName已经存在，但是boundCategory没勾选，就会返回False
                            //这种情况会导致不创建Parameter，但是补充勾选Category
                            if (category.Id.IntegerValue.Equals((int)boundCategory))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }


        //https://www.revitapidocs.com/2023/9942b791-2892-0658-303e-abf99675c5a6.htm
        /// <summary>
        /// An enumerated type listing all of the built-in parameter groups supported by Autodesk Revit. 
        /// such as ["PG_GRAPHICS", "PG_IDENTITY_DATA", "PG_CONSTRAINTS"] 
        /// </summary>
        /// <returns name="BuiltInParameterGroup">BuiltInParameterGroup list</returns> 
        public static List<BuiltInParameterGroup> BuiltInParameterGroup_ZTN()
        {
            return Enum.GetValues(typeof(BuiltInParameterGroup)).Cast<BuiltInParameterGroup>().ToList();
        }


        //https://www.revitapidocs.com/2023/9942b791-2892-0658-303e-abf99675c5a6.htm
        /// <summary>
        /// An enumerated type listing all of the built-in parameter groups supported by Autodesk Revit. 
        /// such as ["PG_GRAPHICS", "PG_IDENTITY_DATA", "PG_CONSTRAINTS"] 
        /// </summary>
        /// <returns name="BuiltInParameterGroup">BuiltInParameterGroup list</returns> 
        private static BuiltInParameterGroup BuiltInParameterGroupAccordingToName_ZTN(string builtInParameterGroupName = "PG_IDENTITY_DATA")
        {
            List<BuiltInParameterGroup> group = Enum.GetValues(typeof(BuiltInParameterGroup)).Cast<BuiltInParameterGroup>().ToList();

            BuiltInParameterGroup builtInParameterGroup = new BuiltInParameterGroup();
            foreach (BuiltInParameterGroup member in group)
            {
                if (member.ToString() == builtInParameterGroupName)
                {
                    builtInParameterGroup = member;
                }

            }

            return builtInParameterGroup;
        }

        //https://www.revitapidocs.com/2023/87de2c69-a5e8-40e3-3d7a-9b18f1fda03a.htm
        /// <summary>
        /// Revit 2021 deprecated the UnitType property and replaced it with the GetSpecTypeId method.
        /// Revit 2022 deprecated the ParameterType property and the GetSpecTypeId method, replacing them both with the GetDataType method.
        /// </summary>
        /// <returns name="SpecTypeIdProperty Name">SpecTypeId Properties Name</returns> 
        /// <returns name="forgeTypeId">Special SpecTypeId Properties, like SpecTypeId.String.Text, or SpecTypeId.Boolean.YesNo</returns> 
        private static Dictionary<string, ForgeTypeId> ForgeTypeIdDict_ZTN()
        {

            List<string> specTypeIdPropertyNameList = new List<string>();
            List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, ForgeTypeId> forgeTypeIdDict = new Dictionary<string, ForgeTypeId>();

            //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
            PropertyInfo[] props = typeof(SpecTypeId).GetProperties();
            foreach (PropertyInfo pi in props)
            {
                specTypeIdPropertyNameList.Add(pi.Name);

                var forgeTypeId = pi.GetValue(null) as ForgeTypeId;
                forgeTypeIdList.Add(forgeTypeId);

                forgeTypeIdDict.Add(pi.Name, forgeTypeId);
            }

            //https://www.revitapidocs.com/2023/cd7d3c3d-b476-9579-1a30-b6b82f1a66d7.htm
            specTypeIdPropertyNameList.Add("Text");
            forgeTypeIdList.Add(SpecTypeId.String.Text);
            specTypeIdPropertyNameList.Add("Material");
            forgeTypeIdList.Add(SpecTypeId.Reference.Material);
            //https://www.revitapidocs.com/2023/3f507360-05c2-b25f-df4f-06f104fb0a6b.htm
            specTypeIdPropertyNameList.Add("Integer");
            forgeTypeIdList.Add(SpecTypeId.Int.Integer);
            specTypeIdPropertyNameList.Add("YesNo");
            forgeTypeIdList.Add(SpecTypeId.Boolean.YesNo);

            forgeTypeIdDict.Add("Text", SpecTypeId.String.Text);
            forgeTypeIdDict.Add("Material", SpecTypeId.Reference.Material);
            forgeTypeIdDict.Add("Integer", SpecTypeId.Int.Integer);
            forgeTypeIdDict.Add("YesNo", SpecTypeId.Boolean.YesNo);

            return forgeTypeIdDict;
        }

        private static ForgeTypeId ForgeTypeIdByParameterTypeName_ZTN(string forgeTypeIdName)
        {
            //var forgeTypeId = new ForgeTypeIdDict_ZTN();
            return ForgeTypeIdDict_ZTN()[forgeTypeIdName];
        }



        /*
        /// <summary>
        /// Revit 2021 deprecated the UnitType property and replaced it with the GetSpecTypeId method.
        /// Revit 2022 deprecated the ParameterType property and the GetSpecTypeId method, replacing them both with the GetDataType method.
        /// </summary>
        /// <returns name="SpecTypeIdProperty Name">SpecTypeId Properties Name</returns> 
        /// <returns name="forgeTypeId">Special SpecTypeId Properties, like SpecTypeId.String.Text, or SpecTypeId.Boolean.YesNo</returns> 
        private static ForgeTypeId ForgeTypeIdByParameterTypeName_ZTN(string forgeTypeIdName)
        {

            List<string> specTypeIdPropertyNameList = new List<string>();
            List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, ForgeTypeId> forgeTypeIdDict = new Dictionary<string, ForgeTypeId>();

            //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
            PropertyInfo[] props = typeof(SpecTypeId).GetProperties();
            foreach (PropertyInfo pi in props)
            {
                specTypeIdPropertyNameList.Add(pi.Name);

                var forgeTypeId = pi.GetValue(null) as ForgeTypeId;
                forgeTypeIdList.Add(forgeTypeId);

                forgeTypeIdDict.Add(forgeTypeIdName, forgeTypeId);
            }

            //https://www.revitapidocs.com/2023/cd7d3c3d-b476-9579-1a30-b6b82f1a66d7.htm
            specTypeIdPropertyNameList.Add("Text");
            forgeTypeIdList.Add(SpecTypeId.String.Text);
            specTypeIdPropertyNameList.Add("Material");
            forgeTypeIdList.Add(SpecTypeId.Reference.Material);
            //https://www.revitapidocs.com/2023/3f507360-05c2-b25f-df4f-06f104fb0a6b.htm
            specTypeIdPropertyNameList.Add("Integer");
            forgeTypeIdList.Add(SpecTypeId.Int.Integer);
            specTypeIdPropertyNameList.Add("YesNo");
            forgeTypeIdList.Add(SpecTypeId.Boolean.YesNo);

            forgeTypeIdDict.Add("Text", SpecTypeId.String.Text);
            forgeTypeIdDict.Add("Material", SpecTypeId.Reference.Material);
            forgeTypeIdDict.Add("Integer", SpecTypeId.Int.Integer);
            forgeTypeIdDict.Add("YesNo", SpecTypeId.Boolean.YesNo);

            return forgeTypeIdDict[forgeTypeIdName];
        }



        //https://jeremytammik.github.io/tbc/a/1908_forgetypeid.html
        //https://www.revitapidocs.com/2022/f38d847e-207f-b59a-3bd6-ebea80d5be63.htm
        //https://forums.autodesk.com/t5/revit-api-forum/internal-definition-parameter-type-missing-in-revit-2023/td-p/11092148
        //https://forums.autodesk.com/t5/revit-api-forum/revit-2022-parametertype-text-to-forgetypeid/m-p/10225741
        //https://forums.autodesk.com/t5/revit-api-forum/forgetypeid-how-to-use/td-p/9439210
        //https://jeremytammik.github.io/tbc/a/1902_2022_sdk_tbc.html
        /// <summary>
        /// An enumerated type listing all of the data type interpretation that Autodesk Revit supports. 
        /// such as ["Text", "Length", "Volume"], works for Revit 2022 and former versions. 
        /// </summary>
        /// <returns name="ParameterType">BuiltInParameterGroup list</returns> 
        public static List<ParameterType> ParameterType_ZTN()
        {
            return Enum.GetValues(typeof(ParameterType)).Cast<ParameterType>().ToList();
        } 
        */

        //https://www.revitapidocs.com/2022/a93168f7-b52d-e97a-7935-50ddcec7fb54.htm
        //https://github.com/DynamoDS/DynamoRevit/blob/217f4a6ed7419920f03e3196d5f3206fb9ccdefb/src/Libraries/RevitNodesUI/RevitTypes.cs#L124
        //https://www.revitapidocs.com/2023/87de2c69-a5e8-40e3-3d7a-9b18f1fda03a.htm
        //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
        //https://www.revitapidocs.com/2022/a4ee065a-21d5-3948-7cd8-1599c4d0dc40.htm
        //https://www.revitapidocs.com/2022/5f0e82b9-cf62-062d-5136-3c4032cca766.htm
        // https://apidocs.co/apps/revit/2022/a2817756-7d35-f9b9-0daf-172010b66ed0.htm
        //https://forums.autodesk.com/t5/revit-api-forum/globalparameter-create-error-spectypeid-revit-22-0-2-392/td-p/10998925
        //https://jeremytammik.github.io/tbc/a/1902_2022_sdk_tbc.html
        /// <summary>
        /// Revit 2021 deprecated the UnitType property and replaced it with the GetSpecTypeId method.
        /// Revit 2022 deprecated the ParameterType property and the GetSpecTypeId method, replacing them both with the GetDataType method.
        /// </summary>
        /// <returns name="SpecTypeIdProperty Name">SpecTypeId Properties Name</returns> 
        /// <returns name="forgeTypeId">Special SpecTypeId Properties, like SpecTypeId.String.Text, or SpecTypeId.Boolean.YesNo</returns> 
        [MultiReturn(new[] { "specTypeIdPropertyName", "forgeTypeId" })]         //"specTypeIdName", "specTypeId", 
        public static object SpecTypeId_ZTN()
        {
            //List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();
            //List<string> forgeTypeIdNameList = new List<string>();
            //List<PropertyInfo> specTypeIdPropertyList = new List<PropertyInfo>();
            //List<PropertyInfo> specTypeIdPropertyList = new List<PropertyInfo>();
            List<string> specTypeIdPropertyNameList = new List<string>();
            //List<ForgeTypeId> specialSpecTypeIdList = new List<ForgeTypeId>();
            List<ForgeTypeId> forgeTypeIdList = new List<ForgeTypeId>();

            //foreach (ForgeTypeId forgeTypeId in UnitUtils.GetAllMeasurableSpecs())
            //{
            //    forgeTypeIdList.Add(forgeTypeId);
            //    forgeTypeIdNameList.Add(forgeTypeId.TypeId);

            //    //specTypeId.TypeId.ToString();
            //}


            //https://forums.autodesk.com/t5/revit-api-forum/convert-parametertype-fixtureunit-to-forgetypeid/td-p/10268488
            PropertyInfo[] props = typeof(SpecTypeId).GetProperties();
            foreach (PropertyInfo pi in props)
            {
                //specTypeIdPropertyList.Add(pi);
                specTypeIdPropertyNameList.Add(pi.Name);

                var forgeTypeId = pi.GetValue(null) as ForgeTypeId;
                forgeTypeIdList.Add(forgeTypeId);
            }

            //https://www.revitapidocs.com/2023/cd7d3c3d-b476-9579-1a30-b6b82f1a66d7.htm
            specTypeIdPropertyNameList.Add("Text");
            forgeTypeIdList.Add(SpecTypeId.String.Text);
            specTypeIdPropertyNameList.Add("Material");
            forgeTypeIdList.Add(SpecTypeId.Reference.Material);
            //https://www.revitapidocs.com/2023/3f507360-05c2-b25f-df4f-06f104fb0a6b.htm
            specTypeIdPropertyNameList.Add("Integer");
            forgeTypeIdList.Add(SpecTypeId.Int.Integer);
            specTypeIdPropertyNameList.Add("YesNo");
            forgeTypeIdList.Add(SpecTypeId.Boolean.YesNo);

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> specTypeIdDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                //{ "specTypeIdProperty",specTypeIdPropertyList},
                { "specTypeIdPropertyName",specTypeIdPropertyNameList},
                //{ "specialSpecTypeId",specialSpecTypeIdList},
                { "forgeTypeId", forgeTypeIdList }
            };

            return specTypeIdDict;
        }

        #endregion
    }
}
