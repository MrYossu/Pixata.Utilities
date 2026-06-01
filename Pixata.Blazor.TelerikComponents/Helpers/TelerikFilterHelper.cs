using System;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Pixata.Blazor.TelerikComponents.Helpers;

public static class TelerikFilterHelper {
  public static CompositeFilterDescriptor CreateSimple(string member, FilterOperator op, object value) =>
    new() {
      FilterDescriptors = [
        new FilterDescriptor {
          Member = member,
          Operator = op,
          Value = value,
          MemberType = value.GetType()
        }
      ]
    };

  public static CompositeFilterDescriptor CreateAnd(string member, params OperatorValue[] ovs) {
    FilterDescriptorCollection filters = [];
    foreach (OperatorValue ov in ovs) {
      filters.Add(new FilterDescriptor(member, ov.Op, ov.Value) { MemberType = ov.Value.GetType() });
    }
    return new CompositeFilterDescriptor {
      LogicalOperator = FilterCompositionLogicalOperator.And,
      FilterDescriptors = filters
    };
  }

  public static CompositeFilterDescriptor CreateOr(string member, params OperatorValue[] ovs) {
    FilterDescriptorCollection filters = [];
    foreach (OperatorValue ov in ovs) {
      filters.Add(new FilterDescriptor(member, ov.Op, ov.Value) { MemberType = ov.Value.GetType() });
    }
    return new CompositeFilterDescriptor {
      LogicalOperator = FilterCompositionLogicalOperator.Or,
      FilterDescriptors = filters
    };
  }

  /// <summary>
  /// For each named DateTime member, modifies an IsEqualTo filter so that filtering by date works even when the value has a non-zero time part
  /// </summary>
  /// <param name="args">The GridReadEventArgs object passed to OnRead</param>
  /// <param name="dateMembers">The name(s) of the DateTime member(s) to adjust</param>
  public static void RemoveTime(GridReadEventArgs args, params string[] dateMembers) {
    foreach (string member in dateMembers) {
      DateTime? filterDate = null;
      foreach (IFilterDescriptor filter in args.Request.Filters) {
        if (filter is CompositeFilterDescriptor cfd) {
          foreach (IFilterDescriptor inner in cfd.FilterDescriptors) {
            if (inner is FilterDescriptor fd) {
              CheckAndFix(fd, member, ref filterDate);
            }
          }
        } else if (filter is FilterDescriptor fd) {
          CheckAndFix(fd, member, ref filterDate);
        }
      }
      if (filterDate is not null) {
        args.Request.Filters.Add(new CompositeFilterDescriptor {
          FilterDescriptors = [
            new FilterDescriptor {
              Member = member,
              MemberType = typeof(DateTime),
              Operator = FilterOperator.IsLessThan,
              Value = filterDate.Value.AddDays(1)
            }
          ]
        });
      }
    }
  }

  private static void CheckAndFix(FilterDescriptor fd, string member, ref DateTime? filterDate) {
    if (fd.Member == member && fd is { Operator: FilterOperator.IsEqualTo, Value: not null }) {
      fd.Operator = FilterOperator.IsGreaterThanOrEqualTo;
      filterDate = (DateTime)fd.Value;
    }
  }
}

public record OperatorValue(FilterOperator Op, object Value);