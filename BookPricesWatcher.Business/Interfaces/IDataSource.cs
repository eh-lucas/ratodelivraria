using Sherlock.Domain.Enums;

namespace Sherlock.Business.Interfaces;
public interface IDataSource
{
    SearchTypeEnum SearchType { get; }
}
