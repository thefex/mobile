using System.Collections.Generic;
using System.Linq;
using Cirrious.FluentLayouts.Touch;
using UIKit;

namespace Toggl.Ross
{
    public static class FluentLayoutExtensions
    {
        public static NSLayoutConstraint[] ToLayoutConstraints(this FluentLayout[] fluentLayouts)
        {
            return fluentLayouts
                   .Where(fluent => fluent != null)
                   .SelectMany(fluent => fluent.ToLayoutConstraints())
                   .ToArray();
        }

        public static NSLayoutConstraint[] ToLayoutConstraints(this IEnumerable<FluentLayout> fluentLayouts)
        {
            return fluentLayouts
                   .Where(fluent => fluent != null)
                   .SelectMany(fluent => fluent.ToLayoutConstraints())
                   .ToArray();
        }
    }
}
