/// <reference types="cypress" />

import { checkSuccess, httpCodes, timeouts } from '../common';
import { EventStream, Fragment, mimeTypes } from '../ldes';
import { CanCheckAvailability } from './interfaces';
import { CollectionsResponse, CollectionResponse, ViewsResponse, ViewResponse } from '../administration';

export class LdesServer implements CanCheckAvailability {
  public static ApplicationStarted = 'Started Application in';

  constructor(public baseUrl: string, private _serviceName?: string) { }

  public get serviceName() {
    return this._serviceName || 'ldes-server';
  }

  private isReady() {
    return cy.request({ url: `${this.baseUrl}/metrics`, failOnStatusCode: false }).then(response => response.isOkStatusCode);
  }

  waitAvailable() {
    return cy.waitUntil(() => this.isReady(), {
      timeout: timeouts.slowAction,
      interval: timeouts.slowCheck, errorMsg:
        `Timed out waiting for container '${this.serviceName}' to be available`
    }).then(() => this);
  }

  getLdes(collection: string) {
    return new EventStream(`${this.baseUrl}/feed/${collection}`).visit();
  }

  sendMemberFile(collection: string, partialFilePath: string, mimeType: string) {
    return cy.readFile(partialFilePath, 'utf8').then(data =>
      cy.request({
        method: 'POST',
        url: `${this.baseUrl}/${collection}`,
        headers: { 'Content-Type': mimeType },
        body: data
      }));
  }

  checkRootFragmentMutable(ldes: string, view: string) {
    return this.getLdes(ldes)
      .then(ldes => new Fragment(ldes.viewUrl(view)).visit())
      .then(view => new Fragment(view.relation.link).visit())
      .then(fragment => fragment.expectMutable());
  }

  sendConfiguration(testPartialPath: string): any {
    const cmd = `sh ${testPartialPath}/server/seed.sh`;
    cy.log(cmd).exec(cmd, { log: true, failOnNonZeroExit: false })
      .then(result => checkSuccess(result).then(success => expect(success).to.be.true));
  }

  configureLdesFromTurtleContent(body: string) {
    cy.request({
      method: 'POST',
      url: `${this.baseUrl}/admin/api/v1/collection`,
      headers: { 'Content-Type': 'text/turtle' },
      body: body,
    }).then(response => { expect(response.status).to.equal(201); });
  }

  configureViewFromTurtleContent(body: string, collection: string) {
    cy.request({
      method: 'POST',
      url: `${this.baseUrl}/admin/api/v1/collection/${collection}/view`,
      headers: { 'Content-Type': 'text/turtle' },
      body: body,
    }).then(response => { expect(response.status).to.equal(201); });
  }

  removeView(collection: string, view: string) {
    cy.request({
      method: 'DELETE',
      url: `${this.baseUrl}/admin/api/v1/collection/${collection}/view/${view}`,
    }).then(response => { expect(response.status).to.equal(200); });
  }

  getCollections() {
    return new CollectionsResponse(`${this.baseUrl}/admin/api/v1/collection`).visit({ mimeType: mimeTypes.trig });
  }

  getCollection(collectionName: string) {
    return new CollectionResponse(`${this.baseUrl}/admin/api/v1/collection/${collectionName}`).visit();
  }

  getUnknownCollection(collectionName: string) {
    return new CollectionResponse(`${this.baseUrl}/admin/api/v1/collection/${collectionName}`).waitForResponseCode(httpCodes.NOT_FOUND);
  }

  getViews(collectionName: string) {
    return new ViewsResponse(`${this.baseUrl}/admin/api/v1/collection/${collectionName}/view`).visit({ mimeType: mimeTypes.trig });
  }

  getView(collectionName: string, viewName: string) {
    return new ViewResponse(`${this.baseUrl}/admin/api/v1/collection/${collectionName}/view/${viewName}`).visit();
  }

  getUnknownCollectionView(collectionName: string, viewName: string) {
    return new ViewResponse(`${this.baseUrl}/admin/api/v1/collection/${collectionName}/view/${viewName}`).waitForResponseCode(httpCodes.NOT_FOUND);
  }

  defineCollection(body: string, contentType: string, responseStatus: number = httpCodes.CREATED): any {
    cy.request({
      method: 'POST',
      url: `${this.baseUrl}/admin/api/v1/collection`,
      headers: { 'Content-Type': contentType },
      body: body,
      failOnStatusCode: false,
    }).then(response => { expect(response.status).to.equal(responseStatus); });
  }

  defineCollectionView(collectionName: string, body: string, contentType: string, responseStatus: number = httpCodes.CREATED): any {
    cy.request({
      method: 'POST',
      url: `${this.baseUrl}/admin/api/v1/collection/${collectionName}/view`,
      headers: { 'Content-Type': contentType },
      body: body,
      failOnStatusCode: false,
    }).then(response => { expect(response.status).to.equal(responseStatus); });
  }

  deleteCollection(collectionName: string, responseStatus: number = httpCodes.OK) {
    cy.request({
      method: 'DELETE',
      url: `${this.baseUrl}/admin/api/v1/collection/${collectionName}`,
      failOnStatusCode: false,
    }).then(response => { expect(response.status).to.equal(responseStatus); });
  }

  deleteView(collectionName: string, viewName: string, responseStatus: number = httpCodes.OK) {
    cy.request({
      method: 'DELETE',
      url: `${this.baseUrl}/admin/api/v1/collection/${collectionName}/view/${viewName}`,
      failOnStatusCode: false,
    }).then(response => { expect(response.status).to.equal(responseStatus); });
  }

}
