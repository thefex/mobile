using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Helpers
{
    public static class Util
    {
        /// <summary>
        /// Logs message and absorbs exception if ILogger is not found
        /// </summary>
        public static void Log(LogLevel severity, string tag, string message)
        {
            try
            {
                var logger = ServiceContainer.Resolve<ILogger>();
                switch (severity)
                {
                    case LogLevel.Info:
                        logger.Info(tag, message);
                        break;
                    case LogLevel.Debug:
                        logger.Debug(tag, message);
                        break;
                    case LogLevel.Error:
                        logger.Debug(tag, message);
                        break;
                    case LogLevel.Warning:
                        logger.Warning(tag, message);
                        break;
                }
            }
            catch
            {
                // Do nothing
            }
        }

        public static string GetName<T> (T enumCase)
        {
            return Enum.GetName(typeof(T), enumCase);
        }

        public static string GetPropertyName<T,TProp>(Expression<Func<T,TProp>> expr)
        {
            var memberExpr = expr.Body as MemberExpression;
            var member = memberExpr?.Member as PropertyInfo;

            if (memberExpr == null || member == null)
                throw new ArgumentException($"Expression {expr} should be a property.");

            return member.Name;
        }

        public static Task<bool> AwaitPredicate(Func<bool> predicate, double interval = 100, double timeout = 5000)
        {
            var tcs = new TaskCompletionSource<bool> ();

            double timePassed = 0;
            var timer = new System.Timers.Timer(interval)  { AutoReset = true };
            timer.Elapsed += (s, e) =>
            {
                timePassed += interval;
                if (timePassed >= timeout)
                {
                    timer.Stop();
                    tcs.SetResult(false);
                }
                else
                {
                    var success = predicate();
                    if (success)
                    {
                        timer.Stop();
                        tcs.SetResult(true);
                    }
                }
            };
            timer.Start();

            return tcs.Task;
        }

        // From http://stackoverflow.com/a/3669020/3922220
        public static void SafeInvoke<T> (this EventHandler<T> evt, object sender, T e)
        {
            if (evt != null)
            {
                evt(sender, e);
            }
        }

        public static EitherGroup<TL, TR> Split<TL, TR> (this IEnumerable<Either<TL, TR>> items)
        {
            var leftList = new List<TL> ();
            var rightList = new List<TR> ();
            foreach (var item in items)
            {
                item.Match(leftList.Add, rightList.Add);
            }
            return new EitherGroup<TL, TR> (leftList, rightList);
        }
    }

    public class Either<TL, TR>
    {
        readonly TL _left;
        readonly TR _right;
        readonly bool _isLeft;

        Either(bool isLeft, TL left, TR right)
        {
            _isLeft = isLeft;
            _left = left;
            _right = right;
        }

        public static Either<TL, TR> Left(TL left)
        {
            return new Either<TL, TR> (true, left, default (TR));
        }

        public static Either<TL, TR> Right(TR right)
        {
            return new Either<TL, TR> (false, default (TL), right);
        }

        public T Match<T> (Func<TL, T> left, Func<TR, T> right)
        {
            return _isLeft ? left(_left) : right(_right);
        }

        public void Match(Action<TL> left, Action<TR> right)
        {
            if (_isLeft)
            {
                left(_left);
            }
            else
            {
                right(_right);
            }
        }

        public async Task MatchAsync(Func<TL, Task> left, Action<TR> right)
        {
            if (_isLeft)
            {
                await left(_left);
            }
            else
            {
                right(_right);
            }
        }

        public Either<TL2, TR2> Select<TL2, TR2> (Func<TL, TL2> left, Func<TR, TR2> right)
        {
            return _isLeft ? Either<TL2, TR2>.Left(left(_left)) : Either<TL2, TR2>.Right(right(_right));
        }

        public Either<T, TR> CastLeft<T> ()
        {
            return _isLeft
                   ? Either<T, TR>.Left((T)(object)_left)
                   : Either<T, TR>.Right(_right);
        }

        public Either<TL, T> CastRight<T> ()
        {
            return _isLeft
                   ? Either<TL, T>.Left(_left)
                   : Either<TL, T>.Right((T)(object)_right);
        }

        public Either<TL2, TR2> Cast<TL2, TR2> ()
        {
            return _isLeft
                   ? Either<TL2, TR2>.Left((TL2)(object)_left)
                   : Either<TL2, TR2>.Right((TR2)(object)_right);
        }

        public TL ForceLeft()
        {
            if (_isLeft)
            {
                return _left;
            }
            else
            {
                throw new Exception("This is a Right either");
            }
        }
    }

    public class EitherGroup<TL, TR>
    {
        public IList<TL> Left { get; private set; }
        public IList<TR> Right { get; private set; }

        public EitherGroup(IList<TL> left, IList<TR> right)
        {
            Left = left;
            Right = right;
        }
    }
}
