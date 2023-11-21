using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Dynamo.Graph.Nodes;
using Revit.Elements;
using Revit.GeometryConversion;
using Revit.Schedules;
using RevitServices.Persistence;
using RevitServices.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DynamoLab.Revit.Elements
{
    ////https://www.revitapidocs.com/2023/1d872359-d091-1666-b898-98727c7be03b.htm
    /// <summary>
    /// Class for RebarCoupler
    /// </summary>
    public class RebarCoupler
    {
        private RebarCoupler() { }


        #region "Creat Rebar Coupler"
        //https://www.revitapidocs.com/2023/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
        //https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Discipline_Specific_Functionality_Structural_Engineering_Structural_Model_Elements_Reinforcement_Rebar_Couplers_html
        /// <summary>
        /// Creates a new instance of a Rebar Coupler between two Rebars. Please make sure we pre-place the right coupler in the model before run the Dynamo Graph
        /// otherwise, we could not choose element, then could not get the type id.
        /// </summary>
        /// <param name="dynamoRebarList"> List contains 2 rebars elements or List contains 2 rebar sets select at the Revit model.</param>
        /// <param name="typeId"> type id for coupler.</param>
        /// <returns name="RebarCoupler"> New created instance of a Rebar Coupler element within the project. </returns> 
        [NodeCategory("Create")]  //3 option Create / Actions / Query
        public static global::Revit.Elements.Element CreateRebarCouplerBetween2Rebars(List<global::Revit.Elements.Element> dynamoRebarList, ElementId typeId)
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            List<Rebar> revitRebarList = new List<Rebar>();
            foreach (global::Revit.Elements.Element dynamoRebar in dynamoRebarList)
            {
                Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;
                revitRebarList.Add(revitRebar);
            }

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            Autodesk.Revit.DB.Structure.RebarCoupler rebarCoupler = null;
            // if we have at least 2 dynamoRebarList, create a rebarCoupler between them
            if (revitRebarList.Count > 1)
            {
                //https://www.revitapidocs.com/2023/6186fbd9-2791-01b7-c45e-00480ff50d83.htm
                //https://www.revitapidocs.com/2023/3be68918-5d12-01f9-c267-b44717592bd4.htm
                ReinforcementData rebarData1 = (ReinforcementData)RebarReinforcementData.Create(revitRebarList[0].Id, 0);
                ReinforcementData rebarData2 = (ReinforcementData)RebarReinforcementData.Create(revitRebarList[1].Id, 1);

                //https://www.revitapidocs.com/2023/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
                rebarCoupler = Autodesk.Revit.DB.Structure.RebarCoupler.Create(dynamoDocument, typeId, rebarData1, rebarData2, out RebarCouplerError error);
                if (error != RebarCouplerError.ValidationSuccessfuly)
                {
                    TaskDialog.Show("Revit", "Create Coupler failed: " + error.ToString());
                }

            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return rebarCoupler.ToDSType(true);
        }

        /*
        //https://www.revitapidocs.com/2023/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
        //https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Discipline_Specific_Functionality_Structural_Engineering_Structural_Model_Elements_Reinforcement_Rebar_Couplers_html
        /// <summary>
        /// Creates a new instance of a Rebar Coupler between two Rebars.
        /// </summary>
        /// <param name="dynamoRebarList"> List contains 2 rebars elements or List contains 2 rebar sets select at the Revit model.</param>
        /// <returns name="RebarCoupler"> New created instance of a Rebar Coupler element within the project. </returns> 
        [NodeCategory("Create")]  //3 option Create / Actions / Query
        public static global::Revit.Elements.Element RebarCouplerBetween2Rebars(List<global::Revit.Elements.Element> dynamoRebarList)  //Autodesk.Revit.DB.Structure.RebarCoupler
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            List<Rebar> revitRebarList = new List<Rebar>();
            foreach (global::Revit.Elements.Element dynamoRebar in dynamoRebarList)
            {
                Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;
                revitRebarList.Add(revitRebar);
            }

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            Autodesk.Revit.DB.Structure.RebarCoupler rebarCoupler = null;
            // if we have at least 2 dynamoRebarList, create a rebarCoupler between them
            if (revitRebarList.Count > 1)
            {
                // get a type id for Coupler, Gets the default family type id with the given family category id.
                // https://www.revitapidocs.com/2023/34d20683-dfea-b1f8-14cf-750611b218ed.htm
                ElementId rebarCouplerElementId = dynamoDocument.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_Coupler));


                if (rebarCouplerElementId != ElementId.InvalidElementId)
                {
                    //FamilySymbol familyType = dynamoDocument.GetElement(rebarCouplerElementId) as FamilySymbol;
                    ////https://www.revitapidocs.com/2023/33042823-a11d-19d4-0d39-f1a4869284a3.htm
                    //string familyName = familyType.Name;

                    //https://www.revitapidocs.com/2023/6186fbd9-2791-01b7-c45e-00480ff50d83.htm
                    //https://www.revitapidocs.com/2023/3be68918-5d12-01f9-c267-b44717592bd4.htm
                    ReinforcementData rebarData1 = (ReinforcementData)RebarReinforcementData.Create(revitRebarList[0].Id, 0);
                    ReinforcementData rebarData2 = (ReinforcementData)RebarReinforcementData.Create(revitRebarList[1].Id, 1);

                    //https://www.revitapidocs.com/2023/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
                    rebarCoupler = Autodesk.Revit.DB.Structure.RebarCoupler.Create(dynamoDocument, rebarCouplerElementId, rebarData1, rebarData2, out RebarCouplerError error);
                    if (error != RebarCouplerError.ValidationSuccessfuly)
                    {
                        TaskDialog.Show("Revit", "Create Coupler failed: " + error.ToString());
                    }

                }
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return rebarCoupler.ToDSType(true);
        } */


        //https://www.revitapidocs.com/2023/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
        /// <summary>
        /// Use a coupler to cap the other end of the first bar.
        /// </summary>
        /// <param name="dynamoRebar"> rebar or rebar set select at the Revit model.</param>
        /// <param name="typeId"> Returns the id of this element's type.</param>
        /// <returns name="RebarCoupler"> New created instance of a Rebar Coupler element at the end of the rebar. </returns> 
        [NodeCategory("Create")]  //3 option Create / Actions / Query
        public static global::Revit.Elements.Element RebarCouplerCap(global::Revit.Elements.Element dynamoRebar, ElementId typeId)
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //UnWrap: get Revit element from the Dynamo-wrapped object
            Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(dynamoDocument);

            // get a type id for the Coupler
            //ElementId rebarCouplerElementId = dynamoDocument.GetDefaultFamilyTypeId(new ElementId(BuiltInCategory.OST_Coupler));

            //https://www.revitapidocs.com/2022/e1df380e-37e8-0bd0-0291-fc4e8c38d747.htm
            // Creates a new instance of RebarReinforcementData. The end of rebar where the coupler stays. This should be 0 or 1
            ReinforcementData rebarData = (ReinforcementData)RebarReinforcementData.Create(revitRebar.Id, 1);

            // Use a coupler to cap the other end of the first bar.
            Autodesk.Revit.DB.Structure.RebarCoupler rebarCoupler = Autodesk.Revit.DB.Structure.RebarCoupler.Create(dynamoDocument, typeId, rebarData, null, out RebarCouplerError error);
            if (error != RebarCouplerError.ValidationSuccessfuly)
            {
                TaskDialog.Show("Revit", "Create Coupler failed: " + error.ToString());
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return rebarCoupler.ToDSType(true);
        }

        #endregion




        #region "Select existing Rebar Coupler family symbol and elements, get typeId information"
        /// <summary>
        /// Find all Structural Rebar Coupler instances in the Document by using category filter
        /// </summary>
        /// <returns name="Rebar Coupler"> select all the rebar coupler element in the model. </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        [MultiReturn(new[] { "ElementTypeId", "Element" })] 
        public static Dictionary<string, object> GetAllRebarCouplerElements()
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Coupler);

            // Apply the filter to the elements in the active dynamoDocument
            // Use shortcut WhereElementIsNotElementType() to find wall instances only
            FilteredElementCollector collector = new FilteredElementCollector(dynamoDocument);
            IList<Autodesk.Revit.DB.Element> rebarCouplers = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements();

            List<ElementId> eleTypeIds = new List<ElementId>();
            List<global::Revit.Elements.Element> elems = new List<global::Revit.Elements.Element>();

            foreach (Autodesk.Revit.DB.Element rebarCoupler in rebarCouplers)
            {
                //https://www.revitapidocs.com/2023/671c33f6-169b-17ca-583b-42f9df50ace5.htm
                //https://www.revitapidocs.com/2023/cc66ca8e-302e-f072-edca-d847bcf14c86.htm
                ElementId elemTypeId = rebarCoupler.GetTypeId();
                global::Revit.Elements.Element elem = rebarCoupler.ToDSType(false);


                eleTypeIds.Add(elemTypeId); ;
                elems.Add(elem);
            }


            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> rebarCouplerDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "ElementTypeId", eleTypeIds},
                { "Element", elems},
            };
            return rebarCouplerDict;


        }

    
        //https://forums.autodesk.com/t5/revit-api-forum/family-instance-filter/td-p/7287113
        //https://forums.autodesk.com/t5/revit-api-forum/i-want-to-get-elements-inside-a-familyinstance/td-p/8918485
        /// <summary>
        /// Got the corresponding Rebar Coupler type, from the input rebar coupler family name and family type name.
        /// </summary>
        /// <param name="familyName"> rebar coupler family name.</param>
        /// <param name="familyTypeName"> rebar coupler family Type name.</param>
        /// <returns name="Family Type"> get the family type of rebar coupler. </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        public static global::Revit.Elements.FamilyType GetRebarCouplerTypeByName(string familyName = "M_Standard Coupler",string familyTypeName = "CPL16M") //
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //This works to find all FamilySymbol of family
            //List<Autodesk.Revit.DB.FamilySymbol> fs = new FilteredElementCollector(dynamoDocument).OfClass(typeof(FamilySymbol))
            //  .OfCategory(BuiltInCategory.OST_Coupler)
            //  .Cast<Autodesk.Revit.DB.FamilySymbol>()
            //  .Where(x => x.FamilyName.Equals(familyName)) // family
            //  .ToList<Autodesk.Revit.DB.FamilySymbol>(); 

           Autodesk.Revit.DB.FamilySymbol fs = new FilteredElementCollector(dynamoDocument).OfClass(typeof(FamilySymbol))
              .OfCategory(BuiltInCategory.OST_Coupler)
              .Cast<Autodesk.Revit.DB.FamilySymbol>()
              .Where(x => x.FamilyName.Equals(familyName)) // family
              .FirstOrDefault(x => x.Name == familyTypeName); //as Autodesk.Revit.DB.FamilySymbol

            global::Revit.Elements.FamilyType dynamoFamilyType = (global::Revit.Elements.FamilyType)fs.ToDSType(false);
            return dynamoFamilyType;
        }

        /// <summary>
        /// From selected Rebar got the corresponding Rebar Coupler type, from the input rebar coupler family name.
        /// </summary>
        /// <param name="dynamoRebar"> rebar selected in the model, which will be used to find the corresponding rebar coupler (share same rebar nominal diameter).</param>
        /// <param name="familyName"> rebar coupler family name.</param>
        /// <returns name="Family Type"> get the family type of rebar coupler. </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        public static global::Revit.Elements.FamilyType GetRebarCouplerTypeByRebarInfor(global::Revit.Elements.Element dynamoRebar, string familyName = "M_Standard Coupler")
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //get RebarBarType from selected Rebar
            //https://forums.autodesk.com/t5/revit-api-forum/get-property-value-of-rebar-bar-diameter-revit-api/td-p/5748929
            Rebar revitRebar = (Rebar)dynamoRebar.InternalElement;
            Autodesk.Revit.DB.Structure.RebarBarType revitBarType = (RebarBarType)dynamoDocument.GetElement(revitRebar.GetTypeId());

            //Autodesk.Revit.DB.Structure.RebarBarType revitBarType = (Autodesk.Revit.DB.Structure.RebarBarType)barType.InternalElement;
            var getDocUnits = dynamoDocument.GetUnits();
            //var getDisplayUnits = getDocUnits.GetFormatOptions(UnitType.UT_Length).DisplayUnits;
            var getDisplayUnits = getDocUnits.GetFormatOptions(Autodesk.Revit.DB.SpecTypeId.Length).GetUnitTypeId();

            double barDiameter = Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(revitBarType.BarNominalDiameter, getDisplayUnits);

            string barDia = Math.Round(barDiameter).ToString();

            //Autodesk.Revit.DB.Parameter columnLengthParameter = column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM);
            //double columnLength = columnLengthParameter.AsDouble();

            Autodesk.Revit.DB.FamilySymbol fs = new FilteredElementCollector(dynamoDocument).OfClass(typeof(FamilySymbol))
               .OfCategory(BuiltInCategory.OST_Coupler)
               .Cast<Autodesk.Revit.DB.FamilySymbol>()
               .Where(x => x.FamilyName.Equals(familyName)) // family
               .FirstOrDefault(x => x.Name.Contains(barDia)); //as Autodesk.Revit.DB.FamilySymbol

            global::Revit.Elements.FamilyType dynamoFamilyType = (global::Revit.Elements.FamilyType)fs.ToDSType(false);
            return dynamoFamilyType;
        }

        /// <summary>
        /// Returns all elements of the specific family type.
        /// </summary>
        /// <param name="dynamoFamilyType"> rebar coupler family type or family symbol in Revit API.</param>
        /// <returns name="Element"> all elements of the family type. </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        public static List<global::Revit.Elements.Element> AllElementsOfRebarCouplerType(global::Revit.Elements.FamilyType dynamoFamilyType) 
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            Autodesk.Revit.DB.FamilySymbol familySymbol = (Autodesk.Revit.DB.FamilySymbol)dynamoFamilyType.InternalElement;

            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Coupler);

            // Apply the filter to the elements in the active dynamoDocument
            // Use shortcut WhereElementIsNotElementType() to find wall instances only
            FilteredElementCollector collector = new FilteredElementCollector(dynamoDocument);
            List<Autodesk.Revit.DB.Element> rebarCouplers = collector.WherePasses(filter)
                .WhereElementIsNotElementType()
                .Where(x => x.GetTypeId().IntegerValue == familySymbol.Id.IntegerValue)
                .ToList <Autodesk.Revit.DB.Element>(); ;

            List<global::Revit.Elements.Element> dynamoElements = new List<global::Revit.Elements.Element> ();
            foreach (Autodesk.Revit.DB.Element ele in rebarCouplers)
            {
                dynamoElements.Add(ele.ToDSType(false));
            }
            return dynamoElements;
        }



        /// <summary>
        /// Returns the identifier of this element's type.
        /// </summary>
        /// <param name="dynamoElement"> rebar coupler selected in Revit model.</param>
        /// <returns name="ElementId"> Returns the identifier of this element's type.. </returns> 
        [NodeCategory("Query")]  //3 option Create / Actions / Query
        public static ElementId GetRebarCouplerTypeId(global::Revit.Elements.Element dynamoElement)
        {
            Autodesk.Revit.DB.Element familySymbol = dynamoElement.InternalElement;
            //https://www.revitapidocs.com/2023/cc66ca8e-302e-f072-edca-d847bcf14c86.htm
            ElementId eleId = familySymbol.GetTypeId();
            return eleId;
        }


        //https://www.revitapidocs.com/2023/cc66ca8e-302e-f072-edca-d847bcf14c86.htm
        //https://adndevblog.typepad.com/aec/2012/11/accessing-the-family-type-of-an-element-using-api.html
        //https://stackoverflow.com/questions/60747624/how-to-get-family-types-from-category-using-revit-api
        /// <summary>
        /// get all family symbol (type) of rebar coupler, and corresponding information
        /// </summary>
        /// <returns name="FamilyType"> rebar coupler family symbol name. </returns> 
        /// <returns name="FamilyTypeName"> rebar coupler family symbol name. </returns> 
        /// <returns name="Family"> rebar coupler familyl name. </returns> 
        /// <returns name="FamilyName"> rebar coupler familyl name. </returns> 
        /// <returns name="FamilyTypeId"> rebar coupler familyl name. </returns> 
        [NodeCategory("Actions")]  //3 option Create / Actions / Query
        [MultiReturn(new[] { "FamilyType", "FamilyTypeName", "Family", "FamilyName", "FamilyTypeId" })]  //, "FamilySymbolElementId" 
        public static Dictionary<string, object> GetRebarCouplerType()   //List<global::Revit.Elements.Element>
        {
            //Get application and dynamoDocument objects
            Document dynamoDocument = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            FilteredElementCollector collector = new FilteredElementCollector(dynamoDocument);
            //ICollection<Autodesk.Revit.DB.Element> rebarCouplers = collector.OfClass(typeof(Autodesk.Revit.DB.FamilyInstance)).OfCategory(BuiltInCategory.OST_Coupler).ToElements();
            ICollection<Autodesk.Revit.DB.Element> rebarCouplers = collector.OfClass(typeof(Autodesk.Revit.DB.FamilySymbol)).OfCategory(BuiltInCategory.OST_Coupler).ToElements();

            //List<global::Revit.Elements.Element> eleList = new List<global::Revit.Elements.Element>();
            //List<ElementId> familySymbolElementId = new List<ElementId>();
            List<global::Revit.Elements.FamilyType> familyType = new List<global::Revit.Elements.FamilyType>();
            List<string> familySymbolName = new List<string>();
            List<global::Revit.Elements.Family> family = new List<global::Revit.Elements.Family>();
            List<string> familyName = new List<string>();
            List<ElementId> familyTypeIds = new List<ElementId>();

            foreach (Autodesk.Revit.DB.Element rebarCoupler in rebarCouplers)
            {
                //https://www.revitapidocs.com/2023/7d050d03-9364-8656-3df7-71fc149d9d73.htm
                Autodesk.Revit.DB.FamilySymbol rebarCouplerFamilySymbol = rebarCoupler as Autodesk.Revit.DB.FamilySymbol;

                //https://www.revitapidocs.com/2023/33042823-a11d-19d4-0d39-f1a4869284a3.htm
                //https://www.revitapidocs.com/2023/cc66ca8e-302e-f072-edca-d847bcf14c86.htm
                //Returns the identifier of this element's type.
                ElementId familyTypeId = rebarCoupler.GetTypeId();
                familyTypeIds.Add(familyTypeId); 

                if (null == rebarCouplerFamilySymbol.GetTypeId())
                {
                    TaskDialog.Show("Revit", "No symbol found in rebarCoupler element: " + rebarCouplerFamilySymbol.Name);
                }
                else
                {
                    global::Revit.Elements.FamilyType dynamoFamilyType= (global::Revit.Elements.FamilyType)rebarCouplerFamilySymbol.ToDSType(false);
                    global::Revit.Elements.Family dynamoFamily = (global::Revit.Elements.Family)rebarCouplerFamilySymbol.Family.ToDSType(false);

                    familyType.Add(dynamoFamilyType);
                    familySymbolName.Add(rebarCouplerFamilySymbol.Name);
                    family.Add(dynamoFamily);
                    familyName.Add(rebarCouplerFamilySymbol.Family.Name);
                }
            }

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> rebarCouplerDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "FamilyType", familyType },
                { "FamilyTypeName", familySymbolName  },

                { "Family", family },
                { "FamilyName", familyName },

                { "FamilyTypeId", familyTypeIds },
            };
            return rebarCouplerDict;


            //// get all the truss types
            //// there's no truss type in the active dynamoDocument
            //FilteredElementCollector filteredElementCollector = new FilteredElementCollector(m_activeDocument.Document);
            //filteredElementCollector.OfClass(typeof(FamilySymbol));
            //filteredElementCollector.OfCategory(BuiltInCategory.OST_Truss);
            //IList<TrussType> trussTypes = filteredElementCollector.Cast<TrussType>().ToList<TrussType>();

            //if (null == trussTypes || 0 == trussTypes.Count)
            //{
            //    TaskDialog.Show("Load Truss Type", "Please load at least one truss type into your project.");
            //    this.Close();
            //}

            //foreach (TrussType trussType in trussTypes)
            //{
            //    if (null == trussType)
            //    {
            //        continue;
            //    }

            //    String trussTypeName = trussType.get_Parameter
            //            (BuiltInParameter.SYMBOL_FAMILY_AND_TYPE_NAMES_PARAM).AsString();
            //    this.TrussTypeComboBox.Items.Add(trussTypeName);
            //    m_trussTypes.Add(trussType);
            //}
        }

        #endregion
    }
}
