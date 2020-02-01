#pragma warning disable 1587
/**
 * Copyright 2019 Wingify Software Pvt. Ltd.
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VWOSdk
{
    internal class OperandEvaluator
    {
        private static string GROUPING_PATTERN = @"/^(.+?)\((.*)\)/";
        private static string WILDCARD_PATTERN = @"/(^\*|^)(.+?)(\*$|$)/";

        internal OperandEvaluator() {}
        public bool evaluateOperand(Dictionary<string, dynamic> operandData, Dictionary<string, dynamic> customVariables) {
            var operandKey = operandData.Keys.First();
            string operand = operandData[operandKey];
            // Retrieve corresponding custom_variable value from custom_variables
            var customVariablesValue = customVariables.ContainsKey(operandKey) ? customVariables[operandKey] : null;

            // Pre process custom_variable value
            customVariablesValue = this.processCustomVariablesValue(customVariablesValue);

            // Pre process operand value
            var procecessedOperands = this.processOperandValue(operand);
            var operandType = procecessedOperands[0];
            var operandValue = procecessedOperands[1];

            // Process the customVariablesValue and operandValue to make them of same type
            string[] trueTypesData = this.convertToTrueTypes(operandValue, customVariablesValue);
            operandValue = trueTypesData[0];
            customVariablesValue = trueTypesData[1];

            switch (operandType) {
                case Constants.OperandValueTypes.CONTAINS:
                    return this.contains(operandValue, customVariablesValue);
                case Constants.OperandValueTypes.STARTS_WITH:
                    return this.startsWith(operandValue, customVariablesValue);
                case Constants.OperandValueTypes.ENDS_WITH:
                    return this.endsWith(operandValue, customVariablesValue);
                case Constants.OperandValueTypes.LOWER:
                    return this.lower(operandValue, customVariablesValue);
                case Constants.OperandValueTypes.REGEX:
                    return this.regex(operandValue, customVariablesValue);
                default:
                    // Default is case of equals to
                    return this.equals(operandValue, customVariablesValue);
            }
        }

        private string processCustomVariablesValue(dynamic customVariableValue) {
            if (customVariableValue == null || customVariableValue.ToString().Length == 0) return "";
            if (customVariableValue == true || customVariableValue == false) {
                customVariableValue = customVariableValue ? Constants.OperandValueBooleanTypes.TRUE : Constants.OperandValueBooleanTypes.FALSE;
            }
            return customVariableValue.ToString();
        }

        private string[] processOperandValue(string operand) {
            var seperatedOperand = this.seperateOperand(operand);
            var operandTypeName = seperatedOperand[0];
            var operandValue = seperatedOperand[1];
            var operandType = typeof(Constants.OperandValueTypesName).GetField(operandTypeName.ToUpper(), BindingFlags.NonPublic | BindingFlags.Static).GetValue(null).ToString();
            string startingStar = "";
            string endingStar = "";

            if (operandTypeName == Constants.OperandValueTypesName.WILDCARD) {
                Match match = Regex.Match(operand, OperandEvaluator.WILDCARD_PATTERN);
                if (match.Success) {
                    startingStar = match.Groups[1].Value;
                    operandValue = match.Groups[2].Value;
                    endingStar = match.Groups[3].Value;
                }
                if (startingStar.Length > 0 && endingStar.Length > 0) {
                    operandType = Constants.OperandValueTypes.CONTAINS;
                } else if (startingStar.Length > 0) {
                    operandType = Constants.OperandValueTypes.STARTS_WITH;
                } else if (endingStar.Length > 0) {
                    operandType = Constants.OperandValueTypes.ENDS_WITH;
                } else {
                    operandType = Constants.OperandValueTypes.EQUALS;
                }
            }

            // In case there is an abnormal patter, it would have passed all the above if cases, which means it
            // Should be equals, so set the whole operand as operand value and operand type as equals
            if (operandType.Length == 0) {
                return new string[] { Constants.OperandValueTypes.EQUALS, operand };
            } else {
                return new string[] { operandType, operandValue };
            }
        }

        private string[] seperateOperand(string operand) {
            Match match = Regex.Match(operand, OperandEvaluator.GROUPING_PATTERN);
            if (match.Success) {
                return new string[] { match.Groups[1].Value, match.Groups[2].Value };
            }
            return new string[] { Constants.OperandValueTypesName.EQUALS, operand };
        }

        private string[] convertToTrueTypes(dynamic operatorValue, dynamic customVariableValue) {
            try {
                var trueTypeOperatorValue = float.Parse(operatorValue, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                var trueTypeCustomVariablesValue = float.Parse(customVariableValue, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                if (trueTypeOperatorValue == Math.Floor(trueTypeOperatorValue)) trueTypeOperatorValue = Int16.Parse(trueTypeOperatorValue);
                if (trueTypeOperatorValue == Math.Floor(trueTypeCustomVariablesValue)) trueTypeCustomVariablesValue = Int16.Parse(trueTypeCustomVariablesValue);
                return new string[] { trueTypeOperatorValue, trueTypeOperatorValue };
            } catch {
                return new string[] { operatorValue, customVariableValue };
            }
        }

        private bool contains(string operandValue, string customVariablesValue) {
            return customVariablesValue.Contains(operandValue);
        }
        private bool startsWith(string operandValue, string customVariablesValue) {
            return customVariablesValue.StartsWith(operandValue);
        }
        private bool endsWith(string operandValue, string customVariablesValue) {
            return customVariablesValue.EndsWith(operandValue);
        }
        private bool lower(string operandValue, string customVariablesValue) {
            return customVariablesValue.ToLower() == operandValue.ToLower();
        }
        private bool regex(string operandValue, string customVariablesValue) {
            Match match = Regex.Match(customVariablesValue, operandValue);
            return match.Success;
        }
        private bool equals(string operandValue, string customVariablesValue) {
            return customVariablesValue == operandValue;
        }
    }
}