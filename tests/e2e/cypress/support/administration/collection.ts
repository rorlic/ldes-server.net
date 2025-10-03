/// <reference types="cypress" />

import { Quad_Graph } from 'n3';
import { UrlResponse, rdf, ldes } from '../ldes';

export class CollectionResponse extends UrlResponse {
  get collection(): Quad_Graph {
    var collections = this.store.getSubjects(rdf.type, ldes.EventStream, null);
    expect(collections.length).to.be.lessThan(2);
    return collections.shift();
  }

  get collectionName(): string | undefined {
    const collection = this.collection?.value;
    return !collection ? undefined : collection.substring(collection.lastIndexOf('/') + 1);
  }

  expectDefined(collectionName: string): any {
    expect(this.collectionName).to.equal(collectionName);
  }

}