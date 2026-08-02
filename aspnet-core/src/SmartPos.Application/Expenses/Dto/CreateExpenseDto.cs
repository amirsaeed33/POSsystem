using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using SmartPos.Expenses;

namespace SmartPos.Expenses.Dto
{
    [AutoMapTo(typeof(Expense))]
    public class CreateExpenseDto
    {
        [Required]
        public int BranchId { get; set; }

        public DateTime ExpenseDate { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(Expense.MaxReferenceNoLength)]
        public string ReferenceNo { get; set; }

        [StringLength(Expense.MaxDescriptionLength)]
        public string Description { get; set; }

        [Required]
        public int PaymentAccountId { get; set; }
    }
}
