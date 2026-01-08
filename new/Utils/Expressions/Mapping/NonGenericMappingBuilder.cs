using System.Linq.Expressions;

namespace IvoEngine.Expressions.Mapping
{
    /// <summary>
    /// Non-generic builder for mapping
    /// </summary>
    public class NonGenericMappingBuilder
    {
        private readonly Type _sourceType;
        private readonly Type _targetType;
        private readonly MappingOptions _options;

        public NonGenericMappingBuilder(Type sourceType, Type targetType)
        {
            _sourceType = sourceType;
            _targetType = targetType;
            _options = new MappingOptions(sourceType, targetType);
        }

        /// <summary>
        /// Sets the auto mapping
        /// </summary>
        public NonGenericMappingBuilder WithAutoMapping(AutoMapping autoMapping)
        {
            _options.SetAutoMapping(autoMapping);
            return this;
        }

        /// <summary>
        /// Sets the mapping policy
        /// </summary>
        public NonGenericMappingBuilder WithPolicy(MappingPolicy policy)
        {
            _options.SetPolicy(policy);
            return this;
        }

        /// <summary>
        /// Adds a simple mapping
        /// </summary>
        public NonGenericMappingBuilder Map(string sourceName, string targetName)
        {
            _options.AddMapping(sourceName.MapToProperty(targetName));
            return this;
        }

        /// <summary>
        /// Adds a constant mapping
        /// </summary>
        public NonGenericMappingBuilder MapConstant(string targetName, object value)
        {
            _options.AddMapping(targetName.MapConstant(value));
            return this;
        }

        /// <summary>
        /// Adds a mapping with a function (non-generic version)
        /// </summary>
        public NonGenericMappingBuilder MapWithFunction(string targetName, LambdaExpression func)
        {
            _options.AddMapping(new FunctionMapping
            {
                TargetName = targetName,
                Function = func
            });
            return this;
        }

        /// <summary>
        /// Adds a nested mapping (non-generic version)
        /// </summary>
        public NonGenericMappingBuilder MapNested(string sourceName, string targetName, params PropertyMapping[] nestedMappings)
        {
            _options.AddMapping(sourceName.MapNested(targetName, nestedMappings));
            return this;
        }

        /// <summary>
        /// Adds a nested mapping using a delegate for configuration
        /// </summary>
        public NonGenericMappingBuilder MapNested(string sourceName, string targetName, Type nestedSourceType, Type nestedTargetType, Action<NonGenericMappingBuilder> configureNested)
        {
            var nestedBuilder = new NonGenericMappingBuilder(nestedSourceType, nestedTargetType);
            configureNested(nestedBuilder);
            var nestedOptions = nestedBuilder.Build();
            var nestedMappings = nestedOptions.CreateFinalMappings();

            var nestedMapping = new NestedPropertyMapping
            {
                SourceName = sourceName,
                TargetName = targetName,
                NestedMappings = nestedMappings.Values.ToList()
            };

            _options.AddMapping(nestedMapping);
            return this;
        }

        /// <summary>
        /// Adds a multi-source mapping (non-generic version)
        /// </summary>
        public NonGenericMappingBuilder MapMultiple(string targetName, LambdaExpression combineFunction, params string[] sourceNames)
        {
            _options.AddMapping(new MultiSourceMapping
            {
                TargetName = targetName,
                SourceNames = sourceNames.ToList(),
                CombineFunction = combineFunction
            });
            return this;
        }

        /// <summary>
        /// Adds an explicit PropertyMapping object
        /// </summary>
        public NonGenericMappingBuilder AddMapping(PropertyMapping mapping)
        {
            _options.AddMapping(mapping);
            return this;
        }

        /// <summary>
        /// Creates the final MappingOptions
        /// </summary>
        public MappingOptions Build()
        {
            return _options;
        }
    }

}
