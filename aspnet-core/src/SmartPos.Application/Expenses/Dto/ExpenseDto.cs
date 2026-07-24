using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using SmartPos.Expenses;

namespace SmartPos.Expenses.Dto
{
    [AutoMapFrom(typeof(Expense))]
    [AutoMapTo(typeof(Expense))]
    public class ExpenseDto : EntityDto
    {
        public DateTime ExpenseDate { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(Expense.MaxReferenceNoLength)]
        public string ReferenceNo { get; set; }

        [StringLength(Expense.MaxDescriptionLength)]
        public string Description { get; set; }

        [Required]
        public int PaymentAccountId { get; set; }

        public string PaymentAccountName { get; set; }

        public int ExpenseAccountId { get; set; }

        public string ExpenseAccountName { get; set; }
    }
}
