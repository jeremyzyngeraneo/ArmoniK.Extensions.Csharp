﻿using System;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.Client.Services.Common;

/// <summary>
///   The abstract for client service creation
/// </summary>
public abstract class AbstractClientService : IDisposable
{
  /// <summary>
  ///   The default constructor with properties information
  /// </summary>
  /// <param name="properties"></param>
  public AbstractClientService(Properties properties)
  {
    LoggerFactory = CreateLogFactory(properties);

    ResultHandlerDictionary = new ConcurrentDictionary<string, IServiceInvocationHandler>();
  }

  /// <summary>
  ///   The result dictionary to return result
  /// </summary>
  protected ConcurrentDictionary<string, IServiceInvocationHandler> ResultHandlerDictionary { get; set; }

  /// <summary>
  ///   The properties to get LoggerFactory or to override it
  /// </summary>
  protected ILoggerFactory LoggerFactory { get; set; }

  /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
  public abstract void Dispose();


  /// <summary>
  ///   Method to build ILoggerFactory
  /// </summary>
  /// <param name="props"></param>
  /// <returns></returns>
  protected ILoggerFactory CreateLogFactory(Properties props)
    => new LoggerFactory(new[]
                         {
                           new SerilogLoggerProvider(new LoggerConfiguration().ReadFrom.Configuration(props.Configuration)
                                                                              .CreateLogger()),
                         });
}