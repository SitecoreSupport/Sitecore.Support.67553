// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateItem.cs" company="Sitecore A/S">
//   Copyright (C) 2010 by Sitecore A/S
// </copyright>
// <summary>
//   Create Item save action
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Support.Form.Submit
{
  using System;
  using System.Collections.Generic;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Data.Managers;
  using Sitecore.Diagnostics;
  using Sitecore.Form.Core.Configuration;
  using Sitecore.Form.Core.Utility;
  using Sitecore.Forms.Core.Data;
  using Sitecore.SecurityModel;
  using Sitecore.WFFM.Abstractions.Actions;
  using Sitecore.WFFM.Abstractions.Dependencies;
  using Sitecore.WFFM.Abstractions.Shared;
  using Sitecore.WFFM.Actions.Base;

  public class CreateItem : WffmSaveAction
  {
    private readonly IResourceManager resourceManager;

    public CreateItem()
      : this(DependenciesManager.ResourceManager)
    {
    }

    public CreateItem(IResourceManager resourceManager)
    {
      this.resourceManager = resourceManager;
      CheckSecurity = false;
    }

    /// <summary>
    /// Executes the specified formId.
    /// </summary>
    /// <param name="formId">The formId.</param>
    /// <param name="adaptedFields">The adapted fields.</param>
    /// <param name="actionCallContext">The action context.</param>
    /// <param name="data">The data.</param>
    public override void Execute(ID formId, AdaptedResultList adaptedFields, ActionCallContext actionCallContext = null, params object[] data)
    {
      SecurityDisabler disabler = null;
      if (!CheckSecurity)
      {
        disabler = new SecurityDisabler();
      }
      try
      {
        CreateItemByFields(formId, adaptedFields);
      }
      finally
      {
        if (disabler != null)
        {
          disabler.Dispose();
        }
      }
    }

    protected virtual void CreateItemByFields(ID formid, AdaptedResultList fields)
    {
      if (StaticSettings.MasterDatabase == null)
      {
        DependenciesManager.Logger.Warn("The Create Item action : the master database is unavailable", this);
      }

      TemplateItem templateItem = StaticSettings.MasterDatabase.GetTemplate(Template);
      Error.AssertNotNull(templateItem, string.Format(resourceManager.GetString("NOT_FOUND_TEMPLATE"), Template));

      Item target = StaticSettings.MasterDatabase.GetItem(Destination);
      Error.AssertNotNull(target, string.Format(resourceManager.GetString("NOT_FOUND_ITEM"), Destination));

      using (new Workflows.WorkflowContextStateSwitcher(Workflows.WorkflowContextState.Enabled))
      {
        Item item = ItemManager.CreateItem(templateItem.Name, target, templateItem.ID);

        var mapFields = StringUtil.ParseNameValueCollection(Mapping, '|', '=');

        item.Editing.BeginEdit();
        foreach (AdaptedControlResult result in fields)
        {
            if (mapFields[result.FieldID] != null)
            {
                string itemFieldID = mapFields[result.FieldID];
                if (item.Fields[itemFieldID] != null)
                {
                    var fieldItem = new FieldItem(StaticSettings.ContextDatabase.GetItem(result.FieldID));
                    string value = result.Value;
                    value = string.Join("|", new List<string>(FieldReflectionUtil.GetAdaptedListValue(fieldItem, value, false)).ToArray());

                    item.Fields[itemFieldID].Value = value;
                    if (itemFieldID == Sitecore.FieldIDs.DisplayName.ToString())
                    {
                        item.Name = Data.Items.ItemUtil.ProposeValidItemName(result.Value);
                    }
                }
                else
                {
                    DependenciesManager.Logger.Warn(string.Format("The Create Item action : the template does not contain field: {0}", itemFieldID), this);
                }
            }
        }
        item.Editing.EndEdit();
      } 
    }

    public bool CheckSecurity
    {
      get;
      set;
    }

    public string Mapping
    {
      get;
      set;
    }

    public string Destination
    {
      get;
      set;
    }

    public string Template
    {
      set;
      get;
    }

  }
}
