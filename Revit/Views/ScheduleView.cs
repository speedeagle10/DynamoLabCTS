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

namespace DynamoLab.Revit.Views
{
    /// <summary>
    /// Utility class that contains methods of view schedule creation and schedule sheet instance creation.
    /// </summary>
    public class ScheduleView
    {
        /// <summary>
        /// Create a view schedule of ViewSchedule category and add schedule field, filter and sorting/grouping field to it.
        /// </summary>
        /// <param name="categoryName">Name of the Element Category.</param>
        /// <param name="scheduleName">Name of the New created schedule.</param>
        /// <param name="fieldNames">Name of the New created schedule.</param>
        /// <returns name="Schedule">new created schedule(s) with selected Field</returns> 
        [NodeCategory("Create")]  //3 option Create / Actions / Query
        public static ViewSchedule CreateSchedules_ZTN(string categoryName, string scheduleName, List<string> fieldNames)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories = document.Settings.Categories;
            //https://www.revitapidocs.com/2022/a8dc01fc-7c56-2fd8-8340-a7fbf7bcc7b4.htm
            Autodesk.Revit.DB.Category category = categories.get_Item(categoryName);
            BuiltInCategory builtInCategory = (BuiltInCategory)category.Id.IntegerValue;

            //Create an empty view schedule of viewSchedule category.
            ViewSchedule schedule = ViewSchedule.CreateSchedule(document, new ElementId(builtInCategory));
            document.Regenerate();
            schedule.Name = scheduleName;
            foreach (string fieldName in fieldNames)
            {
                AddRegularFieldToSchedule_ZTN(document, schedule, fieldName);
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        /// <summary>
        /// map from Dynamo Category to BuildinCategory.
        /// </summary>
        /// <param name="category">Dynamo Category.</param>
        /// <returns name="BuiltInCategory">BuiltIn Category</returns> 
        /// <returns name="BuiltInCategory Name">BuiltIn Category name</returns>
        /// <returns name="Category Name">BuiltIn Category name</returns>
        [NodeCategory("Create")] //3 option Create / Actions / Query
        [MultiReturn(new[] { "builtInCategory", "builtInCategoryName", "categoryName" })]
        public static Dictionary<string, object> Category2BuiltInCategory_ZTN(global::Revit.Elements.Category category)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories = document.Settings.Categories;
            Autodesk.Revit.DB.Category category4Schedule = categories.get_Item(category.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)category4Schedule.Id.IntegerValue;
            string builtInCategoryName = builtInCategory.ToString();

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            //https://www.revitapidocs.com/2023/a055809c-b743-d17f-aef2-31cfa208ecc1.htm
            Dictionary<string, object> categoryDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "builtInCategory", builtInCategory},
                { "builtInCategoryName", builtInCategoryName},
                { "categoryName", category.Name},
            };

            return categoryDict;
        }


        /// <summary>
        /// Create a view schedule of ViewSchedule category and add schedule field, filter and sorting/grouping field to it.
        /// </summary>
        /// <param name="category">Schedule Category.</param>
        /// <param name="scheduleName">Name of the New created schedule.</param>
        /// <param name="fieldNames">Name of the New created schedule.</param>
        /// <param name="includeElementsInLinks">Check the option Include Elements In Links.</param>
        /// <returns name="Schedule">new created schedule(s) with selected Field</returns> 
        [NodeCategory("Create")] //3 option Create / Actions / Query
        public static ViewSchedule CreateSchedules_IncludeElementsInLinks_ZTN(global::Revit.Elements.Category category, string scheduleName, List<string> fieldNames, bool includeElementsInLinks = true)
        {
            //Get application and document objects
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //https://forums.autodesk.com/t5/revit-api-forum/how-to-get-all-elements-from-autodesk-revit-db-category/td-p/10133176
            // Get the category object using the category name
            Autodesk.Revit.DB.Categories categories = document.Settings.Categories;
            Autodesk.Revit.DB.Category category4Schedule = categories.get_Item(category.Name);
            BuiltInCategory builtInCategory = (BuiltInCategory)category4Schedule.Id.IntegerValue;

            //Create an empty view schedule of viewSchedule category.
            ViewSchedule schedule = ViewSchedule.CreateSchedule(document, new ElementId(builtInCategory));
            document.Regenerate();
            schedule.Name = scheduleName;

            //Check the option Include Elements In Links to include information from linked models.
            schedule.Definition.IncludeLinkedFiles = includeElementsInLinks;

            foreach (string fieldName in fieldNames)
            {
                AddRegularFieldToSchedule_ZTN(document, schedule, fieldName);
            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        private static void AddRegularFieldToSchedule_ZTN(Document doc, ViewSchedule schedule, string fieldName)
        {
            ScheduleDefinition definition = schedule.Definition;

            // Find a matching SchedulableField
            Autodesk.Revit.DB.SchedulableField schedulableField =
                definition.GetSchedulableFields().FirstOrDefault<Autodesk.Revit.DB.SchedulableField>(sf => sf.GetName(doc) == fieldName);

            if (schedulableField != null)
            {
                // Add the found field
                definition.AddField(schedulableField);
            }

        }

        /// <summary>
        /// Change Schedule header to a new name.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="newHeaderName">Name of the New created schedule.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        [NodeCategory("Actions")] //3 option Create / Actions / Query
        public static Autodesk.Revit.DB.ViewSchedule SetScheduleHeader_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string newHeaderName)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;


            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();

            //Get header section
            //https://www.revitapidocs.com/2018/1c7a694b-73c0-8d15-7817-5620b6e83098.htm
            TableSectionData tsd = schedule.GetTableData().GetSectionData(SectionType.Header);

            //Set text to the first header cell
            tsd.SetCellText(tsd.FirstRowNumber, tsd.FirstColumnNumber, newHeaderName);

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        // https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Basic_Interaction_with_Revit_Elements_Views_View_Types_TableView_ViewSchedule_Working_with_ViewSchedule_html

        /// <summary>
        /// Add filter to selected schedule.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <param name="filterType">FilterType of the scheduled field.</param>
        /// <param name="filterInformation">filter information of the scheduled field.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        public static Autodesk.Revit.DB.ViewSchedule AddFilter_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule,
            string fieldName, ScheduleFilterType filterType, string filterInformation)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            //Remove filter if needed
            //https://www.revitapidocs.com/2019/57b69056-4175-3de4-511f-c1a1af280300.htm

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();
            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    //https://www.revitapidocs.com/2023/6ec07804-d396-ad9b-d0b8-08b37b3b9ae7.htm
                    Autodesk.Revit.DB.ScheduleFilter filter = new Autodesk.Revit.DB.ScheduleFilter(fieldId, filterType, filterInformation);
                    //ScheduleFilterType.Contains
                    definition.AddFilter(filter);
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        //https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Basic_Interaction_with_Revit_Elements_Views_View_Types_TableView_ViewSchedule_Working_with_ViewSchedule_html

        /// <summary>
        /// sort selected field at selected schedule.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <param name="ascendingOrDescending">choose sort option: true means Ascending, false means Descending.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        [NodeCategory("Actions")] //3 option Create / Actions / Query
        public static Autodesk.Revit.DB.ViewSchedule SortSchedule_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string fieldName, bool ascendingOrDescending = false)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            // clear the sort setting before assign new rules
            //https://www.revitapidocs.com/2019/645645fa-52b8-b747-e3d4-b5428962a4bc.htm
            // If you did not include this line, Revit will add new sort rules for the field and this may cause problem
            // in case users don't want to clear sort filed, we can remove this line or add conditional statement
            definition.ClearSortGroupFields();

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();
            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    if (ascendingOrDescending == false)
                    {
                        // Build sort/group field.
                        ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(fieldId, ScheduleSortOrder.Descending);

                        // Add the sort/group field
                        definition.AddSortGroupField(sortGroupField);
                    }
                    else
                    {
                        // Build sort/group field.
                        ScheduleSortGroupField sortGroupField = new ScheduleSortGroupField(fieldId, ScheduleSortOrder.Ascending);

                        // Add the sort/group field
                        definition.AddSortGroupField(sortGroupField);
                    }
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        //https://www.revitapidocs.com/2015/3890f745-6f24-f81a-9f8f-d8b47c8e3f94.htm

        /// <summary>
        /// change field heading at selected schedule.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldNameList">Name of the scheduled field.</param>
        /// <param name="newFieldNameList"> new Name of the scheduled field.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        [NodeCategory("Actions")] //3 option Create / Actions / Query
        public static Autodesk.Revit.DB.ViewSchedule OverrideColumnHearder_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, List<string> fieldNameList, List<string> newFieldNameList)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();

            for (int i = 0; i < fieldNameList.Count; i++)
            {
                string fieldName = fieldNameList[i];
                string newFieldName = newFieldNameList[i];
                foreach (ScheduleFieldId fieldId in sourceFieldOrder)
                {
                    Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);


                    //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                    if (foundField.GetName() == fieldName)
                    {
                        foundField.ColumnHeading = newFieldName;
                    }
                }

            }

            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;
        }

        //https://www.revitapidocs.com/2015/3890f745-6f24-f81a-9f8f-d8b47c8e3f94.htm

        /// <summary>
        /// Using the ScheduleField.IsHidden property to hide selected field.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <returns name="Schedule">schedule(s) with new header</returns> 
        [NodeCategory("Actions")] //3 option Create / Actions / Query
        public static Autodesk.Revit.DB.ViewSchedule HideColumn_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string fieldName)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            //elements creation and modification has to be inside of a transaction
            TransactionManager.Instance.EnsureInTransaction(document);

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            // clear the sort setting before assign new rules
            //https://www.revitapidocs.com/2019/645645fa-52b8-b747-e3d4-b5428962a4bc.htm
            // If you did not include this line, Revit will add new sort rules for the field and this may cause problem
            // in case users don't want to clear sort filed, we can remove this line or add conditional statement
            definition.ClearSortGroupFields();

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();

            // Refresh data to be sure that schedule is ready for Header Change
            schedule.RefreshData();
            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    foundField.IsHidden = true;
                }
            }
            //End Transaction
            TransactionManager.Instance.TransactionTaskDone();

            return schedule;

        }


        /// <summary>
        /// Display all available ScheduleFilterType, you can choose according to Index. 
        /// Possible Error message: The filter value is not valid for the field and filter type.
        /// </summary>
        /// <returns name="ScheduleFilterType">schedule(s) filter type list</returns> 
        public static List<ScheduleFilterType> ScheduleFilterType_ZTN()
        {
            return Enum.GetValues(typeof(ScheduleFilterType)).Cast<ScheduleFilterType>().ToList();
        }


        /// <summary>
        /// Using the ScheduleField.IsHidden property to hide selected field.
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <param name="fieldName">Name of the scheduled field.</param>
        /// <returns name="SscheduleFieldParameterID">The ID of the parameter displayed by the field.</returns> 
        /// <returns name="ScheduleFieldID">The ID of the field in the containing ScheduleDefinition</returns>
        /// <returns name="ScheduleFieldIndex">The index of the field in the containing ScheduleDefinition.</returns>
        /// <returns name="ScheduleColumnHeading">The column heading text.</returns>
        /// <returns name="ScheduleFieldType">The type of data displayed by the field.</returns>
        [MultiReturn(new[] { "scheduleFieldParameterID", "scheduleFieldID", "scheduleFieldIndex", "scheduleColumnHeading", "scheduleFieldType" })]
        public static Dictionary<string, object> ScheduleFieldMap_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule, string fieldName)
        {
            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            IList<ScheduleFieldId> sourceFieldOrder = definition.GetFieldOrder();
            Autodesk.Revit.DB.ScheduleField foundField2 = null;

            foreach (ScheduleFieldId fieldId in sourceFieldOrder)
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                //if (foundField.ColumnHeading == fieldName) This works if we had not change the column header
                if (foundField.GetName() == fieldName)
                {
                    foundField2 = foundField;
                }
            }

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            //https://www.revitapidocs.com/2023/a055809c-b743-d17f-aef2-31cfa208ecc1.htm
            Dictionary<string, object> scheduleFieldDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "scheduleFieldParameterID", foundField2.ParameterId},
                { "scheduleFieldID", foundField2.FieldId },
                { "scheduleFieldIndex", foundField2.FieldIndex },
                { "scheduleColumnHeading", foundField2.ColumnHeading },
                { "scheduleFieldType", foundField2.FieldType }
            };

            return scheduleFieldDict;
        }

        //https://adndevblog.typepad.com/aec/2015/04/revitapi-scheduledefinitiongetschedulablefields-returns-more-fields-than-ui.html
        /// <summary>
        /// Return all schedulable fields of the selected Schedule. 
        /// Remove  shared or project parameter from field
        /// </summary>
        /// <returns name="SchedulableField">schedulable field of the selected schedule</returns> 
        public static List<Autodesk.Revit.DB.SchedulableField> SchedulableField_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            List<Autodesk.Revit.DB.SchedulableField> scheduleFields = new List<Autodesk.Revit.DB.SchedulableField>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            var fields = definition.GetSchedulableFields();
            foreach (var field in fields)
            {
                if (field.ParameterId.IntegerValue < 0)
                {
                    // parameter visible in the UI
                    scheduleFields.Add(field);
                }
                else
                {
                    // shared or project parameter
                }
            }
            return scheduleFields;

        }

        //https://adndevblog.typepad.com/aec/2015/04/revitapi-scheduledefinitiongetschedulablefields-returns-more-fields-than-ui.html
        /// <summary>
        /// Return all schedulable fields of the selected Schedule. 
        /// Remove  shared or project parameter from field
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="SchedulableFieldName">schedulable field name of the selected schedule</returns> 
        public static List<string> SchedulableFieldName_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            List<string> scheduleFields = new List<string>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            var fields = definition.GetSchedulableFields();
            foreach (var field in fields)
            {
                if (field.ParameterId.IntegerValue < 0)
                {
                    // parameter visible in the UI
                    scheduleFields.Add(field.GetName(document));
                }
                else
                {
                    // shared or project parameter
                }
            }
            return scheduleFields;
        }

        /// <summary>
        /// Return all selected schedulable fields of the selected Schedule. 
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="ScheduledField">scheduled Fields of selected Schedule</returns> 
        public static List<Autodesk.Revit.DB.ScheduleField> ScheduledField_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            List<Autodesk.Revit.DB.ScheduleField> scheduledFields = new List<Autodesk.Revit.DB.ScheduleField>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                scheduledFields.Add(foundField);
            }

            return scheduledFields;
        }

        /// <summary>
        /// Return all selected schedulable fields of the selected Schedule. 
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="ScheduledFieldName">scheduled Field name of selected Schedule</returns> 
        public static List<string> ScheduledFieldName_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            List<string> scheduledFields = new List<string>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                scheduledFields.Add(foundField.GetName());
            }

            return scheduledFields;
        }


        /// <summary>
        /// Return all schedulable fields and scheduled fields of the selected Schedule. 
        /// </summary>
        /// <param name="dynamoSchedule">Selected Schedules.</param>
        /// <returns name="SchedulableFieldName">scheduled Field name of selected Schedule</returns> 
        /// <returns name="ScheduledFieldName">scheduled Field name of selected Schedule</returns> 
        [MultiReturn(new[] { "schedulableFields", "scheduledFields" })]
        public static Dictionary<string, object> ScheduleFields_ZTN(global::Revit.Elements.Views.ScheduleView dynamoSchedule)
        {
            //https://forums.autodesk.com/t5/revit-api-forum/new-zerotouch-foray-dynamo-cant-get-active-document/td-p/6768763
            Document document = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;

            List<string> schedulableFields = new List<string>();
            List<string> scheduledFields = new List<string>();

            //UnWrap: get Revit schedule element from the Dynamo-wrapped object
            Autodesk.Revit.DB.ViewSchedule schedule = (Autodesk.Revit.DB.ViewSchedule)dynamoSchedule.InternalElement;
            ScheduleDefinition definition = schedule.Definition;

            foreach (var field in definition.GetSchedulableFields())
            {
                if (field.ParameterId.IntegerValue < 0)
                {
                    // parameter visible in the UI
                    schedulableFields.Add(field.GetName(document));
                }
                else
                {
                    // shared or project parameter
                }
            }

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                Autodesk.Revit.DB.ScheduleField foundField = definition.GetField(fieldId);
                scheduledFields.Add(foundField.GetName());
            }

            // Creating a dictionary, using Dictionary<TKey,TValue> class
            Dictionary<string, object> scheduleFieldDict = new Dictionary<string, object>
            {
                // Adding key/value pairs in the DictionaryUsing Add() method
                { "schedulableFields", schedulableFields },
                { "scheduledFields", scheduledFields }
            };

            return scheduleFieldDict;
        }

       

    }
}
