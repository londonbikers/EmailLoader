using System.Collections.Generic;

namespace MPN.Apollo.EmailLoaderCore.Models
{
	/// <summary>
	/// Represents a content tag.
	/// </summary>
    public class Tag : ITag
    {
		#region accessors
        public string Name { get; set; }
		public IList<string> Synonyms { get; set; }
		#endregion
		
		#region constructors
		public Tag()
		{
		    Synonyms = new List<string>();
		}
		#endregion
    }
}