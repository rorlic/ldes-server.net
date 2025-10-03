/// <reference types="cypress" />

import { When, Then } from "@badeball/cypress-cucumber-preprocessor";
import { server } from "./common_step_definitions";
import { mimeTypes } from "../ldes";
import { httpCodes } from "../common";

const unknownCollection = 'unknown-collection';
const unknownView = 'unknown-view';

Then('no collections are defined', () => {
  server.getCollections().then(collections => collections.expectNone());
})

When('I delete collection {string}', (collectionName: string) => {
  server.deleteCollection(collectionName);
})

When('I delete an unknown collection', () => {
  server.deleteCollection(unknownCollection, httpCodes.NOT_FOUND);
})

Then('the following collections are defined: {string}', (s: string) => {
  var collectionNames = s.split(',');
  server.getCollections().then(response => response.expectAllDefined(collectionNames));
})

Then('I can retrieve collection {string}', (collectionName: string) => {
  server.getCollection(collectionName).then(response => response.expectDefined(collectionName));
})

Then('I cannot retrieve collection {string}', (collectionName: string) => {
  server.getUnknownCollection(collectionName);
})

Then('I cannot retrieve an unknown collection', () => {
  server.getUnknownCollection(unknownCollection);
})

Then('collection {string} is not defined', (collectionName: string) => {
  server.getUnknownCollection(collectionName);
})

When('I define collection {string}', (filePath: string) => {
  cy.fixture(filePath).then((content:string) => server.defineCollection(content, mimeTypes.turtle));
})

When('I define invalid collection {string}', (filePath: string) => {
  cy.fixture(filePath).then((content:string) => server.defineCollection(content, mimeTypes.turtle, httpCodes.BAD_REQUEST));
})

Then('for collection {string} no views are defined other than the event source', (collectionName: string) => {
  server.getViews(collectionName).then(views => views.expectOnlyEventSource());
})

Then('for collection {string} the following views are defined: {string}', (collectionName: string, s: string) => {
  var viewNames = s.split(',');
  server.getViews(collectionName).then(views => views.expectAllDefined(viewNames));
})

When('I define view {string} for collection {string}', (filePath: string, collectionName: string) => {
  cy.fixture(filePath).then((content:string) => server.defineCollectionView(collectionName, content, mimeTypes.turtle));
})

When('I define view {string} for an unknown collection', (filePath: string) => {
  cy.fixture(filePath).then((content:string) => server.defineCollectionView(unknownCollection, content, mimeTypes.turtle, httpCodes.NOT_FOUND));
})

When('I define invalid view {string} for collection {string}', (filePath: string, collectionName: string) => {
  cy.fixture(filePath).then((content:string) => server.defineCollectionView(collectionName, content, mimeTypes.turtle, httpCodes.BAD_REQUEST));
})

Then('I can retrieve the view {string} of collection {string}', (viewName: string, collectionName: string) => {
  server.getView(collectionName, viewName).then(response => response.expectDefined(viewName));
})

Then('I cannot retrieve the view {string} of collection {string}', (viewName: string, collectionName: string) => {
  server.getUnknownCollectionView(collectionName, viewName);
})

Then('I cannot retrieve an unknown view of collection {string}', (collectionName: string) => {
  server.getUnknownCollectionView(collectionName, unknownView);
})

Then('I cannot retrieve the view {string} of an unknown collection', (viewName: string) => {
  server.getUnknownCollectionView(unknownCollection, viewName);
})

When('I delete view {string} for collection {string}', (viewName: string, collectionName: string) => {
  server.deleteView(collectionName, viewName);
})

When('I delete view {string} for an unknown collection', (viewName: string) => {
  server.deleteView(unknownCollection, viewName, httpCodes.NOT_FOUND);
})

When('I delete view an unknown view of collection {string}', (collectionName: string) => {
  server.deleteView(collectionName, unknownView, httpCodes.NOT_FOUND);
})
