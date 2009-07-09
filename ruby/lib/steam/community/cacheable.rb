# This code is free software; you can redistribute it and/or modify it under the
# terms of the new BSD License.
#
# Copyright (c) 2009, Sebastian Staudt

module Cacheable

  def self.included(base) #:nodoc:

    base.extend ClassMethods
    base.class_eval 'class_variable_set :@@cache, {}'
    base.class_eval 'class_variable_set :@@cache_ids, {}'

    class << base
      alias_method :create, :new
    end

  end

  module ClassMethods

    # Defines wich instance variables should be used to index the cached objects
    def cacheable_with_ids(*ids)
      class_variable_set(:@@cache_ids, ids)
    end

    # Returns whether the requested object +id+ is already cached
    def cached?(id)
      if id.is_a? String
        id.downcase!
      end
      class_variable_get(:@@cache).key?(id)
    end

    # Clears the object cache
    def clear_cache
      class_variable_set :@@cache, {}
    end

    # This checks the cache for an existing object. If it exists it is returned.
    # Otherwise a new object is created.
    # Overrides the default constructor.
    def new(id, fetch = true, bypass_cache = false)
      if cached?(id) and !bypass_cache
        object = class_variable_get(:@@cache)[id]
        object.fetch_data if fetch and !object.fetched?
        object
      else
        super(id, fetch)
      end
    end

  end

  attr_reader :fetch_time

  # Creates a new object for the given +id+, either numeric or
  # the custom URL specified by the user. If +fetch+ is +true+ (default),
  # fetch is used to load data into the object.
  def initialize(fetch = true)
    self.fetch if fetch
    cache
  end

  # Saves this object in the cache
  def cache
    cache     = self.class.class_eval 'class_variable_get :@@cache'
    cache_ids = self.class.class_eval 'class_variable_get :@@cache_ids'

    cache_ids.each do |cache_id|
      cache_id_value = instance_variable_get('@' + cache_id.to_s)
      unless cache_id_value.nil? or cache.key?(cache_id_value)
        cache[cache_id_value] = self
      end
    end

    true
  end

  def fetch
    @fetch_time = Time.now
  end

  # Returns whether the data for this SteamID has already been fetched
  def fetched?
    !@fetch_time.nil?
  end
    
end