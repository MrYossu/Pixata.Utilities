using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public static class TelerikFilterHelper {
  public static CompositeFilterDescriptor CreateSimple(string member, FilterOperator op, object value) =>
    new() {
      FilterDescriptors = new FilterDescriptorCollection {
        new FilterDescriptor {
          Member = member,
          Operator = op,
          Value = value,
          MemberType = value.GetType()
        }
      }
    };

  public static CompositeFilterDescriptor CreateAnd(string member, params OperatorValue[] ovs) {
    FilterDescriptorCollection filters = new();
    foreach (OperatorValue ov in ovs) {
      filters.Add(new FilterDescriptor(member, ov.Op, ov.Value));
    }
    return new CompositeFilterDescriptor {
      LogicalOperator = FilterCompositionLogicalOperator.And,
      FilterDescriptors = filters
    };
  }
  public static CompositeFilterDescriptor CreateOr(string member, params OperatorValue[] ovs) {
    FilterDescriptorCollection filters = new();
    foreach (OperatorValue ov in ovs) {
      filters.Add(new FilterDescriptor(member, ov.Op, ov.Value));
    }
    return new CompositeFilterDescriptor {
      LogicalOperator = FilterCompositionLogicalOperator.Or,
      FilterDescriptors = filters
    };
  }
}

public record OperatorValue(FilterOperator Op, object Value);