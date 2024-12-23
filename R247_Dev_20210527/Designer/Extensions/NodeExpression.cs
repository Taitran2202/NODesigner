using DevExpress.Data.Filtering;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Extensions
{
    public class NodeExpression : NodeViewModel
    {
        public virtual IExpressionData ExpressionData { get; }
        public bool EnableExpression { get; set; }
        public string ResultExpression { get; set; }
        public string MessageExpression { get; set; }
        public Delegate ResultLambda { get; set; }
        public Delegate MessageLambda { get; set; }
        public bool BuildExpression()
        {
            if (ExpressionData != null)
            {

                try
                {
                    ResultLambda = ExpressionExtension.CreateExpression(ResultExpression, ExpressionData.Data.GetType().GetGenericArguments()[0]);
                    MessageLambda = ExpressionExtension.CreateExpression(MessageExpression, ExpressionData.Data.GetType().GetGenericArguments()[0]);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }


        }
    }
    public interface IExpressionData
    {
        object Data { get; set; }
    }
    public static class ExpressionExtension
    {
        public static Delegate CreateExpression(string expression, Type datatype)
        {
            return DevExpress.Data.Filtering.CriteriaCompiler.ToLambda(
                     CriteriaOperator.Parse(expression),
                 CriteriaCompilerDescriptor.Get(datatype)).Compile();
        }
    }
    public class IsNaN : ICustomFunctionOperatorBrowsable
    {
        public int MinOperandCount => 1;

        public int MaxOperandCount => 1;

        public string Description => "IsNaN(Column)" + System.Environment.NewLine + "Check if value is a not a number (NaN)";

        public FunctionCategory Category => FunctionCategory.Math;

        public string Name => "IsNaN";

        public object Evaluate(params object[] operands)
        {
            return double.IsNaN((double)operands[0]);
        }

        public bool IsValidOperandCount(int count)
        {
            return true;
        }

        public bool IsValidOperandType(int operandIndex, int operandCount, Type type)
        {
            return true;
        }

        public Type ResultType(params Type[] operands)
        {
            return typeof(double);
        }
    }


}
