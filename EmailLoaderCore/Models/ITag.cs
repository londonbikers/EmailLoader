using System.Collections.Generic;

namespace MPN.Apollo.EmailLoaderCore.Models
{
	/// <summary>
	/// Represents a content tag.
	/// </summary>
    public interface ITag
    {
        string Name { get; set; }
		IList<string> Synonyms { get; set; }
    }
}