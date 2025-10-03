@server @administration
Feature: LDES Server Administration Functionality

  @test-001
  Scenario: 001: Server Can Manage Collections
    Given context 'tests/001.server-admin-collections' is started
    And the LDES server is available
    Then no collections are defined
    
    When I define invalid collection 'test-001/invalid-collection.ttl'
    Then no collections are defined

    When I define collection 'test-001/collection1.ttl'
    Then the following collections are defined: 'collection1'
    And I can retrieve collection 'collection1'
    And I cannot retrieve collection 'collection2'
    And I cannot retrieve an unknown collection
    
    When I define collection 'test-001/collection2.ttl'
    Then the following collections are defined: 'collection1,collection2'
    And I can retrieve collection 'collection2'
    
    When I delete an unknown collection
    Then the following collections are defined: 'collection1,collection2'
    
    When I delete collection 'collection1'
    Then the following collections are defined: 'collection2'
    And I cannot retrieve collection 'collection1'
    
    When I delete collection 'collection2'
    Then no collections are defined


  @test-002
  Scenario: 002: Server Can Manage Views
    Given context 'tests/002.server-admin-views' is started
    And the LDES server is available
    
    When I define collection 'test-002/collection1.ttl'
    Then for collection 'collection1' no views are defined other than the event source
    
    When I define invalid view 'test-002/invalid-view.ttl' for collection 'collection1'
    Then for collection 'collection1' no views are defined other than the event source
    
    When I define view 'test-002/viewA.ttl' for an unknown collection
    Then for collection 'collection1' no views are defined other than the event source

    When I define view 'test-002/viewA.ttl' for collection 'collection1'
    Then for collection 'collection1' the following views are defined: 'viewA'
    And I can retrieve the view 'viewA' of collection 'collection1'
    And I cannot retrieve an unknown view of collection 'collection1'
    And I cannot retrieve the view 'viewA' of an unknown collection
    
    When I define view 'test-002/viewB.ttl' for collection 'collection1'
    Then for collection 'collection1' the following views are defined: 'viewA,viewB'
    And I can retrieve the view 'viewB' of collection 'collection1'
    
    When I delete view 'viewB.ttl' for an unknown collection
    Then for collection 'collection1' the following views are defined: 'viewA,viewB'
    
    When I delete view an unknown view of collection 'collection1'
    Then for collection 'collection1' the following views are defined: 'viewA,viewB'

    When I delete view 'viewA' for collection 'collection1'
    Then for collection 'collection1' the following views are defined: 'viewB'
    And I cannot retrieve the view 'viewA' of collection 'collection1'
    
    When I delete view 'viewB' for collection 'collection1'
    Then for collection 'collection1' no views are defined other than the event source
