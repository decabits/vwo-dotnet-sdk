﻿#pragma warning disable 1587
/**
 * Copyright 2019-2020 Wingify Software Pvt. Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#pragma warning restore 1587

using System.Collections.Generic;

namespace VWOSdk
{
    public partial class VWO : IVWOClient
    {
        private readonly UserProfileAdapter _userProfileService;
        private readonly ICampaignAllocator _campaignAllocator;
        private readonly IVariationAllocator _variationAllocator;
        private readonly ISegmentEvaluator _segmentEvaluator;
        private readonly AccountSettings _settings;
        private readonly IValidator _validator;
        private readonly bool _isDevelopmentMode;

        internal VWO(AccountSettings settings, IValidator validator, IUserProfileService userProfileService, ICampaignAllocator campaignAllocator, ISegmentEvaluator segmentEvaluator, IVariationAllocator variationAllocator, bool isDevelopmentMode)
        {
            this._settings = settings;
            this._validator = validator;
            this._userProfileService = new UserProfileAdapter(userProfileService);
            this._campaignAllocator = campaignAllocator;
            this._variationAllocator = variationAllocator;
            this._isDevelopmentMode = isDevelopmentMode;
            this._segmentEvaluator = segmentEvaluator;
        }

        #region IVWOClient Methods

        /// <summary>
        /// Activates a server-side A/B test for a specified user for a server-side running campaign.
        /// </summary>
        /// <param name="campaignKey">Campaign key to uniquely identify a server-side campaign.</param>
        /// <param name="userId">User ID which uniquely identifies each user.</param>
        /// <param name="options">Dictionary for passing extra parameters to activate</param>
        /// <returns>
        /// The name of the variation in which the user is bucketed, or null if the user doesn't qualify to become a part of the campaign.
        /// </returns>
        public string Activate(string campaignKey, string userId, Dictionary<string, dynamic> options = null)
        {
            if (options == null) options = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> customVariables = options.ContainsKey("custom_variables") ? options["custom_variables"] : null;
            if (this._validator.Activate(campaignKey, userId, options))
            {
                var campaign = this._campaignAllocator.GetCampaign(this._settings, campaignKey);
                if (campaign == null || campaign.Status != Constants.CampaignStatus.RUNNING) {
                    LogErrorMessage.CampaignNotRunning(typeof(IVWOClient).FullName, campaignKey, nameof(Activate));
                    return null;
                }
                if (campaign.Type != Constants.CampaignTypes.VISUAL_AB) {
                    LogErrorMessage.InvalidApi(typeof(IVWOClient).FullName, userId, campaignKey, campaign.Type, nameof(Activate));
                    return null;
                }
                if (campaign.Segments.Count > 0) {
                    if (customVariables == null) {
                        LogInfoMessage.NoCustomVariables(typeof(IVWOClient).FullName, userId, campaignKey, nameof(Activate));
                        customVariables = new Dictionary<string, dynamic>();
                    }
                    if (!this._segmentEvaluator.evaluate(userId, campaignKey, campaign.Segments, customVariables)) {
                        return null;
                    }
                } else {
                    LogInfoMessage.SkippingPreSegmentation(typeof(IVWOClient).FullName, userId, campaignKey, nameof(Activate));
                }
                var assignedVariation = this.AllocateVariation(campaignKey, userId, apiName: nameof(Activate));
                if (assignedVariation.Variation != null)
                {
                    var trackUserRequest = ServerSideVerb.TrackUser(this._settings.AccountId, assignedVariation.Campaign.Id, assignedVariation.Variation.Id, userId, this._isDevelopmentMode);
                    trackUserRequest.ExecuteAsync();
                    return assignedVariation.Variation.Name;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the variation name assigned for the user for the campaign
        /// </summary>
        /// <param name="campaignKey">Campaign key to uniquely identify a server-side campaign.</param>
        /// <param name="userId">User ID which uniquely identifies each user.</param>
        /// <param name="options">Dictionary for passing extra parameters to activate</param>
        /// <returns>
        /// If variation is assigned then variation name, or Null in case of user not becoming part
        /// </returns>
        public string GetVariation(string campaignKey, string userId, Dictionary<string, dynamic> options = null)
        {
            if (options == null) options = new Dictionary<string, dynamic>();
            Dictionary <string, dynamic> customVariables = options.ContainsKey("custom_variables") ? options["custom_variables"] : null;
            if (this._validator.GetVariation(campaignKey, userId, options))
            {
                var campaign = this._campaignAllocator.GetCampaign(this._settings, campaignKey);
                if (campaign == null || campaign.Status != Constants.CampaignStatus.RUNNING) {
                    LogErrorMessage.CampaignNotRunning(typeof(IVWOClient).FullName, campaignKey, nameof(GetVariation));
                    return null;
                }

                if (campaign.Type == Constants.CampaignTypes.FEATURE_ROLLOUT) {
                    LogErrorMessage.InvalidApi(typeof(IVWOClient).FullName, userId, campaignKey, campaign.Type, nameof(GetVariation));
                    return null;
                }

                if (campaign.Segments.Count > 0) {
                    if (customVariables == null) {
                        LogInfoMessage.NoCustomVariables(typeof(IVWOClient).FullName, userId, campaignKey, nameof(GetVariation));
                        customVariables = new Dictionary<string, dynamic>();
                    }
                    if (!this._segmentEvaluator.evaluate(userId, campaignKey, campaign.Segments, customVariables)) {
                        return null;
                    }
                } else {
                    LogInfoMessage.SkippingPreSegmentation(typeof(IVWOClient).FullName, userId, campaignKey, nameof(GetVariation));
                }

                var assignedVariation = this.AllocateVariation(campaignKey, userId, apiName: nameof(GetVariation));
                if (assignedVariation.Variation != null)
                {
                    var trackUserRequest = ServerSideVerb.TrackUser(this._settings.AccountId, assignedVariation.Campaign.Id, assignedVariation.Variation.Id, userId, this._isDevelopmentMode);
                    trackUserRequest.ExecuteAsync();
                    return assignedVariation.Variation.Name;
                }
                return assignedVariation.Variation?.Name;
            }
            return null;
        }

        /// <summary>
        /// Tracks a conversion event for a particular user for a running server-side campaign.
        /// </summary>
        /// <param name="campaignKey">Campaign key to uniquely identify a server-side campaign.</param>
        /// <param name="userId">User ID which uniquely identifies each user.</param>
        /// <param name="goalIdentifier">The Goal key to uniquely identify a goal of a server-side campaign.</param>
        /// <param name="options">Dictionary for passing extra parameters to activate</param>
        /// <returns>
        /// A boolean value based on whether the impression was made to the VWO server.
        /// True, if an impression event is successfully being made to the VWO server for report generation.
        /// False, If userId provided is not part of campaign or when unexpected error comes and no impression call is made to the VWO server.
        /// </returns>
        public bool Track(string campaignKey, string userId, string goalIdentifier, Dictionary<string, dynamic> options = null)
        {
            if (options == null) options = new Dictionary<string, dynamic>();
            string revenueValue = options.ContainsKey("revenue_value") ? options["revenue_value"].ToString() : null;
            Dictionary <string, dynamic> customVariables = options.ContainsKey("custom_variables") ? options["custom_variables"] : null;
            if(this._validator.Track(campaignKey, userId, goalIdentifier, revenueValue, options))
            {
                var campaign = this._campaignAllocator.GetCampaign(this._settings, campaignKey);
                if (campaign == null || campaign.Status != Constants.CampaignStatus.RUNNING) {
                    LogErrorMessage.CampaignNotRunning(typeof(IVWOClient).FullName, campaignKey, nameof(Track));
                    return false;
                }

                if (campaign.Type == Constants.CampaignTypes.FEATURE_ROLLOUT) {
                    LogErrorMessage.InvalidApi(typeof(IVWOClient).FullName, userId, campaignKey, campaign.Type, nameof(Track));
                    return false;
                }

                if (campaign.Segments.Count > 0) {
                    if (customVariables == null) {
                        LogInfoMessage.NoCustomVariables(typeof(IVWOClient).FullName, userId, campaignKey, nameof(Track));
                        customVariables = new Dictionary<string, dynamic>();
                    }
                    if (!this._segmentEvaluator.evaluate(userId, campaignKey, campaign.Segments, customVariables)) {
                        return false;
                    }
                } else {
                    LogInfoMessage.SkippingPreSegmentation(typeof(IVWOClient).FullName, userId, campaignKey, nameof(Track));
                }

                var assignedVariation = this.AllocateVariation(campaignKey, userId, goalIdentifier: goalIdentifier, apiName: nameof(Track));
                var variationName = assignedVariation.Variation?.Name;
                var selectedGoalIdentifier = assignedVariation.Goal?.Identifier;
                if (string.IsNullOrEmpty(variationName) == false)
                {
                    if (string.IsNullOrEmpty(selectedGoalIdentifier) == false)
                    {
                        bool sendImpression = true;
                        if (assignedVariation.Goal.IsRevenueType() && string.IsNullOrEmpty(revenueValue))
                        {
                            sendImpression = false;
                            LogErrorMessage.TrackApiRevenueNotPassedForRevenueGoal(file, goalIdentifier, campaignKey, userId);
                        }
                        else if (assignedVariation.Goal.IsRevenueType() == false)
                        {
                            revenueValue = null;
                        }

                        if (sendImpression)
                        {
                            var trackGoalRequest = ServerSideVerb.TrackGoal(this._settings.AccountId, assignedVariation.Campaign.Id, assignedVariation.Variation.Id, userId, assignedVariation.Goal.Id, revenueValue, this._isDevelopmentMode);
                            trackGoalRequest.ExecuteAsync();
                            return true;
                        }
                    }
                    else
                    {
                        LogErrorMessage.TrackApiGoalNotFound(file, goalIdentifier, campaignKey, userId);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Identifies whether the user becomes a part of feature rollout/test or not.
        /// </summary>
        /// <param name="campaignKey">Campaign key to uniquely identify a server-side campaign.</param>
        /// <param name="userId">User ID which uniquely identifies each user.</param>
        /// <param name="options">Dictionary for passing extra parameters to activate</param>
        /// <returns>
        /// /// A boolean value based on whether the impression was made to the VWO server.
        /// True, if an impression event is successfully being made to the VWO server for report generation.
        /// False, If userId provided is not part of campaign or when unexpected error comes and no impression call is made to the VWO server.
        /// </returns>
        public bool IsFeatureEnabled(string campaignKey, string userId, Dictionary<string, dynamic> options = null)
        {
            if (options == null) options = new Dictionary<string, dynamic>();
            Dictionary <string, dynamic> customVariables = options.ContainsKey("custom_variables") ? options["custom_variables"] : null;
            if (this._validator.IsFeatureEnabled(campaignKey, userId, options))
            {
                var campaign = this._campaignAllocator.GetCampaign(this._settings, campaignKey);
                if (campaign == null || campaign.Status != Constants.CampaignStatus.RUNNING) {
                    LogErrorMessage.CampaignNotRunning(typeof(IVWOClient).FullName, campaignKey, nameof(IsFeatureEnabled));
                    return false;
                }
                if (campaign.Type == Constants.CampaignTypes.VISUAL_AB) {
                    LogErrorMessage.InvalidApi(typeof(IVWOClient).FullName, userId, campaignKey, campaign.Type, nameof(IsFeatureEnabled));
                    return false;
                }

                if (campaign.Segments.Count > 0) {
                    if (customVariables.Count == 0) {
                        LogInfoMessage.NoCustomVariables(typeof(IVWOClient).FullName, userId, campaignKey, nameof(IsFeatureEnabled));
                        customVariables = new Dictionary<string, dynamic>();
                    }
                    if (!this._segmentEvaluator.evaluate(userId, campaignKey, campaign.Segments, customVariables)) {
                        return false;
                    }
                } else {
                    LogInfoMessage.SkippingPreSegmentation(typeof(IVWOClient).FullName, userId, campaignKey, nameof(IsFeatureEnabled));
                }

                var assignedVariation = this.AllocateVariation(campaignKey, userId, apiName: nameof(IsFeatureEnabled));
                if (campaign.Type == Constants.CampaignTypes.FEATURE_TEST) {
                    if (assignedVariation.Variation != null)
                    {
                        var trackUserRequest = ServerSideVerb.TrackUser(this._settings.AccountId, assignedVariation.Campaign.Id, assignedVariation.Variation.Id, userId, this._isDevelopmentMode);
                        trackUserRequest.ExecuteAsync();
                        var result = assignedVariation.Variation.IsFeatureEnabled;

                        if(result)
                        {
                        LogInfoMessage.FeatureEnabledForUser(typeof(IVWOClient).FullName, campaignKey, userId, nameof(IsFeatureEnabled));
                        }
                        else
                        {
                        LogInfoMessage.FeatureNotEnabledForUser(typeof(IVWOClient).FullName, campaignKey, userId, nameof(IsFeatureEnabled));
                        }
                        return result;
                    }
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the feature variable corresponding to the variableKey passed. It typecasts the value to the corresponding value type found in settings_file
        /// </summary>
        /// <param name="campaignKey">Campaign key to uniquely identify a server-side campaign.</param>
        /// <param name="variableKey">Campaign key to uniquely identify a server-side campaign.</param>
        /// <param name="userId">User ID which uniquely identifies each user.</param>
        /// <param name="options">Dictionary for passing extra parameters to activate</param>
        /// <returns>
        /// The name of the variation in which the user is bucketed, or null if the user doesn't qualify to become a part of the campaign.
        /// </returns>
        public dynamic GetFeatureVariableValue(string campaignKey, string variableKey, string userId, Dictionary<string, dynamic> options = null)
        {
            if (options == null) options = new Dictionary<string, dynamic>();
            Dictionary <string, dynamic> customVariables = options.ContainsKey("custom_variables") ? options["custom_variables"] : null;
            var variables = new List<Dictionary<string, dynamic>>();
            var variable = new Dictionary<string, dynamic>();

            if (this._validator.GetFeatureVariableValue(campaignKey, variableKey, userId, options))
            {
                var campaign = this._campaignAllocator.GetCampaign(this._settings, campaignKey);
                if (campaign == null || campaign.Status != Constants.CampaignStatus.RUNNING) {
                    LogErrorMessage.CampaignNotRunning(typeof(IVWOClient).FullName, campaignKey, nameof(GetFeatureVariableValue));
                    return null;
                }

                if (campaign.Type == Constants.CampaignTypes.VISUAL_AB) {
                    LogErrorMessage.InvalidApi(typeof(IVWOClient).FullName, userId,  campaignKey, campaign.Type, nameof(GetFeatureVariableValue));
                    return null;
                }

                if (campaign.Segments.Count > 0)
                {
                    if (customVariables== null)
                    {
                        LogInfoMessage.NoCustomVariables(typeof(IVWOClient).FullName, userId, campaignKey, nameof(GetFeatureVariableValue));
                        customVariables = new Dictionary<string, dynamic>();
                    }
                    if (!this._segmentEvaluator.evaluate(userId, campaignKey, campaign.Segments, customVariables)) {
                        return null;
                    }
                } else
                {
                    LogInfoMessage.SkippingPreSegmentation(typeof(IVWOClient).FullName, userId, campaignKey, nameof(GetFeatureVariableValue));
                }

                var assignedVariation = this.AllocateVariation(campaignKey, userId, apiName: nameof(GetFeatureVariableValue));
                if (campaign.Type == Constants.CampaignTypes.FEATURE_ROLLOUT)
                {
                   variables = campaign.Variables;
                }
                else if (campaign.Type == Constants.CampaignTypes.FEATURE_TEST)
                {
                    if (!assignedVariation.Variation.IsFeatureEnabled)
                    {
                        LogInfoMessage.FeatureNotEnabledForUser(typeof(IVWOClient).FullName, campaignKey, userId, nameof(GetFeatureVariableValue));
                        assignedVariation = this.GetControlVariation(campaign, campaign.Variations.Find(1, (new VariationAllocator()).GetVariationId));
                    }
                    else
                    {
                        LogInfoMessage.FeatureEnabledForUser(typeof(IVWOClient).FullName, campaignKey, userId, nameof(GetFeatureVariableValue));
                    }
                    variables = assignedVariation.Variation.Variables;
                }

                variable = this.GetVariable(variables, variableKey);
                if (variable == null || variable.Count == 0)
                {
                   LogErrorMessage.VariableNotFound(typeof(IVWOClient).FullName, variableKey, campaignKey, campaign.Type, userId, nameof(GetFeatureVariableValue));
                   return null;
                }
                else
                {
                    LogInfoMessage.VariableFound(typeof(IVWOClient).FullName, variableKey, campaignKey, campaign.Type, variable["value"].ToString(), userId, nameof(GetFeatureVariableValue));
                }
                return this._segmentEvaluator.getTypeCastedFeatureValue(variable["value"], variable["type"]);
            }
            return null;
        }

        /// <summary>
        /// Makes a call to our server to store the tagValues
        /// </summary>
        /// <param name="tagKey">key name of the tag</param>
        /// <param name="tagValue">value of the tag</param>
        /// <param name="userId">User ID which uniquely identifies each user.</param>
        /// <returns>
        /// A boolean value based on whether the impression was made to the VWO server.
        /// True, if an impression event is successfully being made to the VWO server for report generation.
        /// False, If userId provided is not part of campaign or when unexpected error comes and no impression call is made to the VWO server.
        /// </returns>
        public bool Push(string tagKey, dynamic tagValue, string userId)
        {
            if (this._validator.Push(tagKey, tagValue, userId)) {
                if((int)tagKey.Length > (Constants.PushApi.TAG_KEY_LENGTH)) {
                    LogErrorMessage.TagKeyLengthExceeded(typeof(IVWOClient).FullName, tagKey, userId, nameof(Push));
                    return false;
                }

                if((int)tagValue.Length > (Constants.PushApi.TAG_VALUE_LENGTH)) {
                    LogErrorMessage.TagValueLengthExceeded(typeof(IVWOClient).FullName, tagValue, userId, nameof(Push));
                    return false;
                }
                var pushRequest = ServerSideVerb.PushTags(this._settings, tagKey, tagValue, userId, this._isDevelopmentMode);
                pushRequest.ExecuteAsync();
                return true;
            }
            return false;
        }


        #endregion IVWOClient Methods

        #region private Methods
        /// <summary>
        /// Allocate variation by checking UserProfileService, Campaign Traffic Allocation and compute UserHash to check variation allocation by bucketing.
        /// </summary>
        /// <param name="campaignKey"></param>
        /// <param name="userId"></param>
        /// <param name="apiName"></param>
        /// <returns>
        /// If Variation is allocated, returns UserAssignedInfo with valid details, else return Empty UserAssignedInfo.
        /// </returns>
        private UserAllocationInfo AllocateVariation(string campaignKey, string userId, string apiName = null)
        {
            UserProfileMap userProfileMap = this._userProfileService.GetUserMap(campaignKey, userId);
            BucketedCampaign selectedCampaign = this._campaignAllocator.Allocate(this._settings, userProfileMap, campaignKey, userId, apiName);
            if (selectedCampaign != null)
            {
                Variation variation = this._variationAllocator.Allocate(userProfileMap, selectedCampaign, userId);
                if (variation != null)
                {
                    LogInfoMessage.VariationAllocated(file, userId, campaignKey, variation.Name);
                    LogDebugMessage.GotVariationForUser(file, userId, campaignKey, variation.Name, nameof(AllocateVariation));

                    this._userProfileService.SaveUserMap(userId, selectedCampaign.Key, variation.Name);
                    return new UserAllocationInfo(variation, selectedCampaign);
                }
            }

            LogInfoMessage.NoVariationAllocated(file, userId, campaignKey);
            return new UserAllocationInfo();
        }

        private UserAllocationInfo GetControlVariation(BucketedCampaign campaign, Variation variation) {
            return new UserAllocationInfo(variation, campaign);
        }

        private Dictionary<string, dynamic> GetVariable(List<Dictionary<string, dynamic>> Variables, string VariableKey) {
            Dictionary<string, dynamic> matchingVariable = Variables.Find(variable => variable.ContainsKey("key") && variable["key"] == VariableKey);
            return matchingVariable;
        }

        /// <summary>
        /// If variation is assigned, allocate the goal using goalIdentifier.
        /// </summary>
        /// <param name="campaignKey"></param>
        /// <param name="userId"></param>
        /// <param name="goalIdentifier"></param>
        /// <returns>
        /// If Variation is allocated and goal with given identifier is found, return UserAssignedInfo with valid information, otherwise, Empty UserAssignedInfo object.
        /// </returns>
        private UserAllocationInfo AllocateVariation(string campaignKey, string userId, string goalIdentifier, string apiName)
        {
            var userAllocationInfo = this.AllocateVariation(campaignKey, userId, apiName);
            if (userAllocationInfo.Variation != null)
            {
                if (userAllocationInfo.Campaign.Goals.TryGetValue(goalIdentifier, out Goal goal))
                    userAllocationInfo.Goal = goal;
                else
                    LogErrorMessage.TrackApiGoalNotFound(file, goalIdentifier, campaignKey, userId);
            }
            else {
                LogErrorMessage.TrackApiVariationNotFound(file, campaignKey, userId);
            }
            return userAllocationInfo;
        }

        #endregion private Methods
    }
}
