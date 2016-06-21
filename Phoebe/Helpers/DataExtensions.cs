using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SQLite.Net.Async;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Helpers
{
    public static class DataExtensions
    {
        /// <summary>
        /// Checks if the two objects share the same type and primary key.
        /// </summary>
        /// <param name="data">Data object.</param>
        /// <param name="other">Other data object.</param>
        public static bool Matches(this CommonData data, object other)
        {
            if (data == other)
            {
                return true;
            }
            if (data == null || other == null)
            {
                return false;
            }
            if (data.GetType() != other.GetType())
            {
                return false;
            }
            return data.Id == ((CommonData)other).Id;
        }

        public static TResult MatchType<T, T1, T2, TResult> (
            this T item, Func<T1, TResult> f1, Func<T2, TResult> f2)
        where T1 : T where T2 : T
        {
            if (item is T1)
            {
                return f1((T1)item);
            }
            if (item is T2)
            {
                return f2((T2)item);
            }
            throw new Exception("Unknown type: " + item.GetType().FullName);
        }

        public static TResult MatchType<T, T1, T2, T3, TResult> (
            this T item, Func<T1, TResult> f1, Func<T2, TResult> f2, Func<T3, TResult> f3)
        where T1 : T where T2 : T where T3 : T
        {
            if (item is T1)
            {
                return f1((T1)item);
            }
            if (item is T2)
            {
                return f2((T2)item);
            }
            if (item is T3)
            {
                return f3((T3)item);
            }
            throw new Exception("Unknown type: " + item.GetType().FullName);
        }

        public static TResult MatchType<T, T1, T2, T3, T4, TResult> (
            this T item, Func<T1, TResult> f1, Func<T2, TResult> f2,
            Func<T3, TResult> f3, Func<T4, TResult> f4)
        where T1 : T where T2 : T where T3 : T where T4 : T
        {
            if (item is T1)
            {
                return f1((T1)item);
            }
            if (item is T2)
            {
                return f2((T2)item);
            }
            if (item is T3)
            {
                return f3((T3)item);
            }
            if (item is T4)
            {
                return f4((T4)item);
            }
            throw new Exception("Unknown type: " + item.GetType().FullName);
        }

        public static TResult MatchType<T, T1, T2, T3, T4, T5, TResult> (
            this T item, Func<T1, TResult> f1, Func<T2, TResult> f2,
            Func<T3, TResult> f3, Func<T4, TResult> f4, Func<T5, TResult> f5)
        where T1 : T where T2 : T where T3 : T where T4 : T where T5 : T
        {
            if (item is T1)
            {
                return f1((T1)item);
            }
            if (item is T2)
            {
                return f2((T2)item);
            }
            if (item is T3)
            {
                return f3((T3)item);
            }
            if (item is T4)
            {
                return f4((T4)item);
            }
            if (item is T5)
            {
                return f5((T5)item);
            }
            throw new Exception("Unknown type: " + item.GetType().FullName);
        }

        public static bool UpdateData<T> (this IList<T> list, T data)
        where T : CommonData
        {
            var updateCount = 0;

            for (var idx = 0; idx < list.Count; idx++)
            {
                if (data.Matches(list [idx]))
                {
                    list [idx] = data;
                    updateCount++;
                }
            }

            return updateCount > 0;
        }

        public static string ToIdString(this CommonData data)
        {
            var id = data.RemoteId.HasValue ? data.RemoteId.ToString() : data.Id.ToString();
            return string.Concat(data.GetType().Name, "#", id);
        }

        public static async Task<bool> ExistWithNameAsync(this AsyncTableQuery<ClientData> query, string name)
        {
            var rows = await query.Where(r => r.Name == name).ToListAsync().ConfigureAwait(false);
            return rows.Count != 0;
        }

        public static async Task<bool> ExistWithNameAsync(this AsyncTableQuery<ProjectData> query, string projectName, Guid clientId)
        {
            List<ProjectData> existingProjects;
            if (clientId != Guid.Empty)
            {
                existingProjects = await query
                                   .Where(r => r.Name == projectName && r.ClientId == clientId)
                                   .ToListAsync().ConfigureAwait(false);
            }
            else
            {
                existingProjects = await query
                                   .Where(r => r.Name == projectName)
                                   .ToListAsync().ConfigureAwait(false);
            }
            return existingProjects.Count != 0;
        }

        // TODO: Check also IsBillable, Tags?
        public static bool IsGroupableWith(this ITimeEntryData data, ITimeEntryData other)
        {
            var groupable = data.ProjectId == other.ProjectId &&
                            string.Compare(data.Description, other.Description, StringComparison.Ordinal) == 0 &&
                            data.TaskId == other.TaskId &&
                            data.UserId == other.UserId &&
                            data.WorkspaceId == other.WorkspaceId;

            if (groupable)
            {
                var date1 = data.StartTime.ToLocalTime().Date;
                var date2 = other.StartTime.ToLocalTime().Date;
                return date1 == date2;
            }
            return false;
        }

        public static TimeSpan GetDuration(this ITimeEntryData data)
        {
            var now = Time.UtcNow;
            if (data.StartTime.IsMinValue())
            {
                return TimeSpan.Zero;
            }

            var duration = (data.StopTime ?? now) - data.StartTime;
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }
            return duration;
        }

        /// <summary>
        /// Change duration of a time entry.
        /// </summary>
        public static void SetDuration(this TimeEntryData data, TimeSpan value, bool isBeingCreatedManually)
        {
            var now = Time.UtcNow;

            if (isBeingCreatedManually)
            {
                if (value == TimeSpan.Zero)
                {
                    data.StartTime = DateTime.MinValue;
                    data.StopTime = null;
                }
                else if (data.StopTime.HasValue)
                {
                    data.StartTime = data.StopTime.Value - value;
                }
                else
                {
                    data.StartTime = now - value;
                    data.StopTime = now;
                }
            }
            if (data.State == TimeEntryState.Finished)
            {
                data.StopTime = data.StartTime + value;
            }
            else // time entry is running
            {
                data.StartTime = now - value;
            }

            data.StartTime = data.StartTime.Truncate(TimeSpan.TicksPerSecond);
            data.StopTime = data.StopTime.Truncate(TimeSpan.TicksPerSecond);
        }

        // GetProperties doesn't get properties from base interfaces, we need this
        // From http://stackoverflow.com/a/2444090/3922220
        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo> ();

                var considered = new List<Type> ();
                var queue = new Queue<Type> ();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) { continue; }

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                                             BindingFlags.FlattenHierarchy
                                             | BindingFlags.Public
                                             | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                                           .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                                      | BindingFlags.Public | BindingFlags.Instance);
        }

        public static bool PublicInstancePropertiesEqual<T> (this T self, T to, params string[] ignore) where T : ICommonData
        {
            Func<object, object, bool> areDifferent = (x, y) => x != y && (x == null || !x.Equals(y));

            Type type = typeof(T);
            var ignoreList = new List<string> (ignore);
            foreach (PropertyInfo pi in type.GetPublicProperties())
            {
                if (!ignoreList.Contains(pi.Name))
                {
                    object selfValue = pi.GetValue(self, null);
                    object toValue = pi.GetValue(to, null);

                    var selfSeq = selfValue as System.Collections.IEnumerable;
                    var toSeq = toValue as System.Collections.IEnumerable;

                    var selfStr = selfValue as string;
                    var toStr = toValue as string;

                    if (selfSeq != null && toSeq != null)
                    {
                        var bothFinished = false;
                        var enum1 = selfSeq.GetEnumerator();
                        var enum2 = toSeq.GetEnumerator();

                        while (!bothFinished)
                        {
                            var firstFinished = !enum1.MoveNext();
                            var secondFinished = !enum2.MoveNext();

                            if (firstFinished && secondFinished)
                            {
                                bothFinished = true;
                            }
                            else if (firstFinished || secondFinished)
                            {
                                return false;
                            }
                            else
                            {
                                if (areDifferent(enum1.Current, enum2.Current))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    else if (selfStr != null || toStr != null)
                    {
                        if (string.IsNullOrEmpty(selfStr) && string.IsNullOrEmpty(toStr))
                            return true;

                        if (areDifferent(selfStr, toStr))
                        {
                            return false;
                        }
                    }
                    else if (areDifferent(selfValue, toValue))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Change StartTime to a TimeEntryData
        /// </summary>
        public static TimeEntryData ChangeStartTime(this TimeEntryData data, DateTime newValue)
        {
            newValue = newValue.ToUtc().Truncate(TimeSpan.TicksPerSecond);
            data.StartTime = newValue;
            return data;
        }

        /// <summary>
        /// Change StopTime to a TimeEntryData
        /// </summary>
        public static TimeEntryData ChangeStoptime(this TimeEntryData data, DateTime? newValue)
        {
            newValue = newValue.ToUtc().Truncate(TimeSpan.TicksPerSecond);
            data.StopTime = newValue;
            return data;
        }
    }
}
