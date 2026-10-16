using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;
using RaceDayApi.Models;
using RaceDayApi.Tests_.Helpers;

namespace RaceDayApi.Tests_
{
    public class UnitTest1
    {
        [Fact]
        public void Result_ShouldStoreValidResultDetails()
        {
            var result = new Result
            {
                ResultId = 1,
                EnrolmentId = 2,
                FinishTime = new TimeSpan(1, 25, 30),
                Position = 1,
                Status = "Finished"
            };

            Assert.Equal(1, result.ResultId);
            Assert.Equal(2, result.EnrolmentId);
            Assert.Equal(new TimeSpan(1, 25, 30), result.FinishTime);
            Assert.Equal(1, result.Position);
            Assert.Equal("Finished", result.Status);
        }

        [Theory]
        [InlineData("Finished")]
        [InlineData("DNF")]
        [InlineData("DSQ")]
        public void Result_ShouldStoreAllowedStatuses(string status)
        {
            var result = new Result
            {
                EnrolmentId = 1,
                Status = status
            };

            Assert.Equal(status, result.Status);
        }

        [Fact]
        public void Result_ShouldAllowOptionalFieldsToBeNull()
        {
            var result = new Result
            {
                EnrolmentId = 1,
                FinishTime = null,
                Position = null,
                Status = "DNF"
            };

            Assert.Null(result.FinishTime);
            Assert.Null(result.Position);
        }

        [Fact]
        public void Result_ShouldFailValidation_WhenStatusIsEmpty()
        {
            var result = new Result
            {
                EnrolmentId = 1,
                Status = ""
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(result);

            bool isValid = Validator.TryValidateObject(
                result,
                validationContext,
                validationResults,
                validateAllProperties: true
            );

            Assert.False(isValid);
            Assert.Contains(
                validationResults,
                error => error.MemberNames.Contains("Status")
            );
        }

        [Fact]
        public void Result_ShouldPassValidation_WhenRequiredFieldsAreProvided()
        {
            var result = new Result
            {
                EnrolmentId = 1,
                Status = "Finished"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(result);

            bool isValid = Validator.TryValidateObject(
                result,
                validationContext,
                validationResults,
                validateAllProperties: true
            );

            Assert.True(isValid);
        }

        [Theory]
        [InlineData("Finished")]
        [InlineData("DNF")]
        [InlineData("DSQ")]
        public void StatusValidator_ShouldAcceptAllowedStatuses(string status)
        {
            bool result = ResultStatusValidator.IsValidStatus(status);

            Assert.True(result);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Cancelled")]
        [InlineData("")]
        [InlineData("   ")]
        public void StatusValidator_ShouldRejectInvalidStatuses(string status)
        {
            bool result = ResultStatusValidator.IsValidStatus(status);

            Assert.False(result);
        }

        [Fact]
        public void StatusValidator_ShouldAcceptLowercaseStatus()
        {
            bool result = ResultStatusValidator.IsValidStatus("finished");

            Assert.True(result);
        }

        [Fact]
        public void StatusValidator_ShouldRejectNullStatus()
        {
            bool result = ResultStatusValidator.IsValidStatus(null);

            Assert.False(result);
        }
    }
}