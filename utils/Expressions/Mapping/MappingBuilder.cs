using System.Linq.Expressions;

namespace Utils.Expressions.Mapping
{
    /// <summary>
    /// Builder class for easier creation of mappings
    /// </summary>
    public class MappingBuilder<TSource, TTarget>
    {
        private readonly MappingOptions _options;

        public MappingBuilder()
        {
            _options = new MappingOptions(typeof(TSource), typeof(TTarget));
        }

        /// <summary>
        /// Sets automatic mapping
        /// </summary>
        public MappingBuilder<TSource, TTarget> WithAutoMapping(AutoMapping autoMapping)
        {
            _options.SetAutoMapping(autoMapping);
            return this;
        }

        /// <summary>
        /// Sets mapping policy
        /// </summary>
        public MappingBuilder<TSource, TTarget> WithPolicy(MappingPolicy policy)
        {
            _options.SetPolicy(policy);
            return this;
        }

        /// <summary>
        /// Adds simple mapping
        /// </summary>
        public MappingBuilder<TSource, TTarget> Map(string sourceName, string targetName)
        {
            _options.AddMapping(sourceName.MapToProperty(targetName));
            return this;
        }

        /// <summary>
        /// Adds constant mapping
        /// </summary>
        public MappingBuilder<TSource, TTarget> MapConstant(string targetName, object value)
        {
            _options.AddMapping(targetName.MapConstant(value));
            return this;
        }

        /// <summary>
        /// Adds mapping with function
        /// </summary>
        public MappingBuilder<TSource, TTarget> MapWithFunction<TResult>(string targetName, Expression<Func<TSource, TResult>> func)
        {
            _options.AddMapping(targetName.MapUsing(func));
            return this;
        }

        /// <summary>
        /// Adds nested mapping using builder
        /// </summary>
        public MappingBuilder<TSource, TTarget> MapNested<TNestedSource, TNestedTarget>(
            string sourceName,
            string targetName,
            Action<MappingBuilder<TNestedSource, TNestedTarget>> configureNested)
        {
            _options.AddMapping(sourceName.MapNested(targetName, configureNested));
            return this;
        }

        /// <summary>
        /// Adds nested mapping
        /// </summary>
        public MappingBuilder<TSource, TTarget> MapNested(string sourceName, string targetName, params PropertyMapping[] nestedMappings)
        {
            _options.AddMapping(sourceName.MapNested(targetName, nestedMappings));
            return this;
        }

        /// <summary>
        /// Adds multi-source mapping
        /// </summary>
        public MappingBuilder<TSource, TTarget> MapMultiple<TResult>(
            string targetName,
            LambdaExpression combineFunction,
            params string[] sourceNames)
        {
            _options.AddMapping(targetName.MapMultiple<TSource, TResult>(combineFunction, sourceNames));
            return this;
        }

        /// <summary>
        /// Creates final MappingOptions
        /// </summary>
        public MappingOptions Build()
        {
            return _options;
        }
    }

}
