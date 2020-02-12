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

using System;
using Moq;
using Xunit;

namespace VWOSdk.Tests
{
    public class UserProfileAdapterTests
    {
        private readonly string MockCampaignKey = "MockCampaignKey";
        private readonly string MockUserId = "MockUserId";
        private readonly string MockVariationName = "MockVariationName";

        [Fact]
        public void GetUserMap_Should_Match_And_Return_Profile_Data_When_LookUp_Returns_Valid_Map()
        {
            var mockUserProfileService = Mock.GetUserProfileService();
            Mock.SetupLookup(mockUserProfileService, GetUserProfileMap());
            UserProfileAdapter userProfileServiceAdapter = new UserProfileAdapter(mockUserProfileService.Object);
            var result = userProfileServiceAdapter.GetUserMap(MockCampaignKey, MockUserId);
            Assert.NotNull(result);
            Assert.Equal(MockUserId, result.UserId);
            Assert.Equal(MockCampaignKey, result.CampaignKey);
            Assert.Equal(MockVariationName, result.VariationName);
        }

        [Fact]
        public void GetUserMap_Should_Return_Null_When_LookUp_Returns_InValid_Map()
        {
            var mockUserProfileService = Mock.GetUserProfileService();
            Mock.SetupLookup(mockUserProfileService, returnValue: null);
            UserProfileAdapter userProfileServiceAdapter = new UserProfileAdapter(mockUserProfileService.Object);
            var result = userProfileServiceAdapter.GetUserMap(MockCampaignKey, MockUserId);
            Assert.Null(result);
        }

        [Fact]
        public void GetUserMap_Should_Return_Null_When_LookUp_Throws_Execption()
        {
            var mockUserProfileService = Mock.GetUserProfileService();
            Mock.SetupLookup(mockUserProfileService, new Exception("Test Method Exception"));
            UserProfileAdapter userProfileServiceAdapter = new UserProfileAdapter(mockUserProfileService.Object);
            var result = userProfileServiceAdapter.GetUserMap(MockCampaignKey, MockUserId);
            Assert.Null(result);

            mockUserProfileService.Verify(mock => mock.Lookup(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            mockUserProfileService.Verify(mock => mock.Lookup(It.Is<string>(val => MockUserId.Equals(val)), It.Is<string>(val => MockCampaignKey.Equals(val))), Times.Once);
        }

        [Fact]
        public void SaveUserMap_Should_Call_Save_With_Provided_Map()
        {
            var mockUserProfileService = Mock.GetUserProfileService();
            UserProfileAdapter userProfileServiceAdapter = new UserProfileAdapter(mockUserProfileService.Object);
            userProfileServiceAdapter.SaveUserMap(MockUserId, MockCampaignKey, MockVariationName);
            mockUserProfileService.Verify(mock => mock.Save(It.IsAny<UserProfileMap>()), Times.Once);
            mockUserProfileService.Verify(mock => mock.Save(It.Is<UserProfileMap>(val => Verify(val))), Times.Once);
        }

        [Fact]
        public void SaveUserMap_Should_Call_Save_With_Provided_Map_And_Should_Not_Throw_Exception_When_Service_Throws_Exception()
        {
            var mockUserProfileService = Mock.GetUserProfileService();
            Mock.SetupSave(mockUserProfileService, new Exception("Test Method Exception."));
            UserProfileAdapter userProfileServiceAdapter = new UserProfileAdapter(mockUserProfileService.Object);
            userProfileServiceAdapter.SaveUserMap(MockUserId, MockCampaignKey, MockVariationName);
            mockUserProfileService.Verify(mock => mock.Save(It.IsAny<UserProfileMap>()), Times.Once);
            mockUserProfileService.Verify(mock => mock.Save(It.Is<UserProfileMap>(val => Verify(val))), Times.Once);
        }

        private bool Verify(UserProfileMap val)
        {
            if(val != null)
            {
                if(val.CampaignKey.Equals(MockCampaignKey) && val.UserId.Equals(MockUserId) && val.VariationName.Equals(MockVariationName))
                    return true;
            }
            return false;
        }

        private UserProfileMap GetUserProfileMap()
        {
            return new UserProfileMap()
            {
                CampaignKey = MockCampaignKey,
                UserId = MockUserId,
                VariationName = MockVariationName
            };
        }
    }
}
