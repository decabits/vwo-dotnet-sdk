#pragma warning disable 1587
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

using System;

namespace VWOSdk
{
    internal class UserProfileAdapter
    {
        private static readonly string file = typeof(UserProfileAdapter).FullName;
        private IUserProfileService _userProfileService;

        public UserProfileAdapter(IUserProfileService userProfileService)
        {
            this._userProfileService = userProfileService;
        }

        /// <summary>
        /// If UserProfileService is provided, Calls Lookup for given UserId and validate the result.
        /// </summary>
        /// <param name="campaignKey"></param>
        /// <param name="userId"></param>
        /// <returns>
        /// Returns userProfileMap if validation is success, else null.
        /// </returns>
        internal UserProfileMap GetUserMap(string campaignKey, string userId)
        {
            if (this._userProfileService == null)
            {
                LogDebugMessage.NoUserProfileServiceLookup(file);
                return null;
            }

            UserProfileMap userMap = TryGetUserMap(userId, campaignKey);

            if (userMap == null || string.IsNullOrEmpty(userMap.CampaignKey)
                || string.IsNullOrEmpty(userMap.VariationName) || string.IsNullOrEmpty(userMap.UserId)
                || string.Equals(userMap.UserId, userId) == false || string.Equals(userMap.CampaignKey, campaignKey) == false)
            {
                LogDebugMessage.NoStoredVariation(file, userId, campaignKey);
                return null;
            }

            LogInfoMessage.GotStoredVariation(file, userMap.VariationName, campaignKey, userId);
            LogDebugMessage.GettingStoredVariation(file, userId, campaignKey, userMap.VariationName);
            return userMap;
        }

        /// <summary>
        /// Calls Lookup within try to suppress any Exception from outside of SDK application.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private UserProfileMap TryGetUserMap(string userId, string campaignKey)
        {
            try
            {
                LogInfoMessage.LookingUpUserProfileService(file, userId, campaignKey);
                return this._userProfileService.Lookup(userId, campaignKey);
            }
            catch (Exception ex)
            {
                LogErrorMessage.LookUpUserProfileServiceFailed(file, userId, campaignKey);
            }

            return null;
        }

        internal void SaveUserMap(string userId, string campaignKey, string variationName)
        {
            if (this._userProfileService == null)
            {
                LogDebugMessage.NoUserProfileServiceSave(file);
                return;
            }

            try
            {
                LogInfoMessage.SavingDataUserProfileService(file, userId);
                this._userProfileService.Save(new UserProfileMap(userId, campaignKey, variationName));
                return;
            }
            catch (Exception ex)
            {
                LogErrorMessage.SaveUserProfileServiceFailed(file, userId);
            }
        }


    }
}
