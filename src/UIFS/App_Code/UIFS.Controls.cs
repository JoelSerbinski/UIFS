using System;
using System.Collections.Generic;
using System.Web;

namespace UIFS
{

    public enum ControlType : int
    {
        Textbox = 0,
        Checkbox = 1,
        List = 2,
        DateTime = 3,
        Number = 4,
        Percentage = 5,
        Range = 6
    }

    public class FormControl
    {
        // Put properties here that apply to ALL controls
        public int id; // the unique db identifier for this control (populated when read from db)
        public int version; // current version of this control
        public string name; // name of control
        public string prompt; // prompt user with
        public string tip; // OnHover popup/title description for this element
        public int ordernum = -1; // the sort order of the control
        public bool required; // whether or not to require the control to be filled out on form submission

        public void UpdateCommonProperties(FormControl newControlProperties)
        {
            // If passed values are blank, do not update
            if (newControlProperties.name != null) { name = newControlProperties.name; }
            if (newControlProperties.prompt != null) { prompt = newControlProperties.prompt; }
            if (newControlProperties.tip != null) { tip = newControlProperties.tip; }
            if (newControlProperties.ordernum != -1) { ordernum = newControlProperties.ordernum; }
        }

        // ---------------------------------------------------------
        // The Controls

        // TEXTBOX: I am not sure whether to just have the 255 and FullText, or to add some variable length options
        // DB STORAGE: length is limited to 255
        public class Textbox : FormControl
        {            
            public int lines = 1; // default
            public int width = 20; // default

            // DB STORAGE: The [FullTextBox] is a VARCHAR(MAX) db field...no length limits
            public bool FullText = false;

        }

        // LIST: The Items are stored in the db as a single string comma separated items [name:value]. The colon character CANNOT BE USED IN THESE VALUES (as it is the separator)
        // DB STORAGE: the chosen value is saved
        public class List : FormControl
        {
            public Item[] Items = new Item[0];
            public listtype type = listtype.radio; // default

            public class Item
            {
                public string name;
                public string value;
            }
            public enum listtype : byte
            {
                radio = 0,
                dropdown = 1,
                slider = 2
            }

            public void RemoveItem(int iItem)
            {
                Item[] NewItems = new Item[Items.Length-1]; // setup new array
                Array.Copy(Items, 0, NewItems, 0, iItem); // copy start
                Array.Copy(Items, iItem + 1, NewItems, iItem, Items.Length - iItem - 1); // copy end
                Items = null;
                Items = new Item[NewItems.Length];
                Items = NewItems;
                NewItems = null;
            }
            public void AddItem(string name, string value)
            {
                for (int t = 0; t < Items.Length; t++)
                { // Check for duplicate name = NOT ALLOWED
                    if (Items[t].name == name) { return; } 
                }
                Array.Resize(ref Items, Items.Length + 1);
                Items[Items.Length - 1] = new Item();
                Items[Items.Length - 1].name = name;
                Items[Items.Length - 1].value = value;
            }
            /* Pass to this routine the controlid you want reordered and the new order number */
            public void ReOrderItem(int iItem, int newOrderNum)
            {
                Item reorderItem = new Item();
                Item[] NewItems = new Item[Items.Length]; // setup new array
                reorderItem = Items[iItem]; // capture item being moved
                RemoveItem(iItem); // remove this item
                Array.Copy(Items, 0, NewItems, 0, newOrderNum); // copy everything up to the NEW item position
                NewItems[newOrderNum] = reorderItem; // copy new item to its position
                Array.Copy(Items, newOrderNum, NewItems, newOrderNum + 1, Items.Length - newOrderNum); // copy everything else 

                Items = null;
                Items = new Item[NewItems.Length];
                Items = NewItems;
                NewItems = null;
            }
            // OUR HOURLY BLOCKs (Predefined)
            public static Item[] HourBlocks = new Item[] {
                new Item{name="(Midnight)12am-1am", value="12am-1am"},
                new Item{name="1am-2am",value="1am-2am"},
                new Item{name="2am-3am",value="2am-3am"},
                new Item{name="3am-4am",value="3am-4am"},
                new Item{name="4am-5am",value="4am-5am"},
                new Item{name="5am-6am",value="5am-6am"},
                new Item{name="6am-7am",value="6am-7am"},
                new Item{name="7am-8am",value="7am-8am"},
                new Item{name="8am-9am",value="8am-9am"},
                new Item{name="9am-10am",value="9am-10am"},
                new Item{name="10am-11am",value="10am-11am"},
                new Item{name="11am-12pm(Noon)",value="11am-12pm"},
                new Item{name="12pm-1pm",value="12pm-1pm"},
                new Item{name="1pm-2pm",value="1pm-2pm"},
                new Item{name="2pm-3pm",value="2pm-3pm"},
                new Item{name="3pm-4pm",value="3pm-4pm"},
                new Item{name="4pm-5pm",value="4pm-5pm"},
                new Item{name="5pm-6pm",value="5pm-6pm"},
                new Item{name="6pm-7pm",value="6pm-7pm"},
                new Item{name="7pm-8pm",value="7pm-8pm"},
                new Item{name="8pm-9pm",value="8pm-9pm"},
                new Item{name="9pm-10pm",value="9pm-10pm"},
                new Item{name="10pm-11pm",value="10pm-11pm"},
                new Item{name="11pm-12am(Midnight)",value="11pm-12am"}
            };

        }

        // CHECKBOX: 
        public class Checkbox : FormControl
        {
            public checkboxtype type = checkboxtype.standard; // checkbox type - default standard
            public bool initialstate = false; // true if checked
            public bool hasinput = false; // will place a textbox field alongside the checkbox
                        
            public enum checkboxtype: byte
            {
                standard = 0, // checkbox...
                YesNo = 1, // Turns into a Yes/No selection
                OnOff = 2  // Turns into a On/Off selection
            }

         }
        
        // DATETIME: can be designated as just a date or just time
        public class DateTime : FormControl
        {
            public datetimetype type = datetimetype.datetime; // default
            public enum datetimetype : byte
            {
                datetime = 0,
                date = 1,
                time = 2
            }
        }
        
        // NUMBER: can set min and max values along with an interval.  can convert to slider control
        public class Number : FormControl
        {
            public decimal min=0; // minimum value
            public decimal max = 999999; // maximum value
            public decimal interval = 0; // the desired separation between selection values
            public bool slider = false; // Will turn the control into a slider
        }


        // PERCENTAGE:  Simple 0 to 100% selection with option to use interval
        public class Percentage : FormControl
        {            
            public int interval = 1; // The distance to separate the percentage selections
        }


        // RANGE: Basically selecting a range with a slider control with various possible values
        public class Range : FormControl
        {
            public Rangetype type;
            public decimal min = 0;
            public decimal max = 0;


            public enum Rangetype : byte
            {
                TimeRange = 1,
                DateRange = 2,
                DateTimeRange = 3,
                Currency = 9,
                MinMax = 10
            }
        }

    }




    /* We need a set of routines to build the jquery to be returned with ajax calls to make the control edit functions operate
     * */
    public class jQuery
    {
        string js="";
        public string List(int ListID)
        {
            js = "<script type='text/javascript'>$(function(){ $('#" + ListID.ToString() + "_sortable').sortable('destroy'); " +
                "$('#" + ListID.ToString() + "_sortable').sortable({placeholder: 'List_Options_List_sortable-highlight', " +
                " update: function(event, ui) {sortindex = $(ui.item).parent().children().index(ui.item); itemid = $(ui.item).attr('value'); Ajax('?cmd=1101&Option=ReOrder&id="+ListID.ToString()+"&item='+ itemid +'&newindex='+ sortindex, 'Control_"+ListID.ToString() +"');}" +
                "}); " +
                " }); </script>";
            return js;
        }
        public string List_AddNew()
        {
            // Need to hide buttons that are used for updating existing controls 
            // (this is really a trick to use the same code for displaying the properties of controls)
            return "<script type='text/javascript'>B_CommonProp=document.getElementById('-1_Save');B_CommonProp.style.display='none'; button = document.getElementById('-1_ListSave');button.style.display='none';</script>";
        }
        public string Textbox_AddNew()
        { 
            // Need to hide buttons that are used for updating existing controls 
            // (this is really a trick to use the same code for displaying the properties of controls)
            return "<script type='text/javascript'>B_CommonProp=document.getElementById('-1_Save');B_CommonProp.style.display='none'; button = document.getElementById('-1_TextboxSave');button.style.display='none';</script>";
        }
        public string Checkbox_AddNew()
        {
            // Need to hide buttons that are used for updating existing controls 
            // (this is really a trick to use the same code for displaying the properties of controls)
            return "<script type='text/javascript'>B_CommonProp=document.getElementById('-1_Save');B_CommonProp.style.display='none'; button = document.getElementById('-1_CheckboxSave');button.style.display='none';</script>";
        }
        public string DateTime_AddNew()
        {
            // Need to hide buttons that are used for updating existing controls 
            // (this is really a trick to use the same code for displaying the properties of controls)
            return "<script type='text/javascript'>B_CommonProp=document.getElementById('-1_Save');B_CommonProp.style.display='none'; button = document.getElementById('-1_DateTimeSave');button.style.display='none';</script>";
        }
        public string Number_AddNew()
        {
            // Need to hide buttons that are used for updating existing controls 
            // (this is really a trick to use the same code for displaying the properties of controls)
            return "<script type='text/javascript'>B_CommonProp=document.getElementById('-1_Save');B_CommonProp.style.display='none'; button = document.getElementById('-1_NumberSave');button.style.display='none';</script>";
        }
        public string Percentage_AddNew()
        {
            // Need to hide buttons that are used for updating existing controls 
            // (this is really a trick to use the same code for displaying the properties of controls)
            return "<script type='text/javascript'>B_CommonProp=document.getElementById('-1_Save');B_CommonProp.style.display='none'; button = document.getElementById('-1_PercentageSave');button.style.display='none';</script>";
        }
        public string Range_AddNew()
        {
            // Need to hide buttons that are used for updating existing controls 
            // (this is really a trick to use the same code for displaying the properties of controls)
            return "<script type='text/javascript'>B_CommonProp=document.getElementById('-1_Save');B_CommonProp.style.display='none'; button = document.getElementById('-1_RangeSave');button.style.display='none';</script>";
        }
    }

}
